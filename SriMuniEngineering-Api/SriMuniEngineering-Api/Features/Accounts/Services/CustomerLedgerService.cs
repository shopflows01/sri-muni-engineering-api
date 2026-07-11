using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

using SriMuniEngineering_Api.Common.Dtos;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface ICustomerLedgerService
{
    Task<CustomerLedgerDto> GetLedgerAsync(Guid customerId, PaginationRequest pagination);
    Task<decimal> GetOutstandingAsync(Guid customerId);
    Task<decimal> GetAdvanceBalanceAsync(Guid customerId);
    Task<CustomerLedgerDto> CreateLedgerAsync(Guid customerId, CreateCustomerLedgerRequest request);
}

public class CustomerLedgerService : ICustomerLedgerService
{
    private readonly AppDbContext _context;

    public CustomerLedgerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerLedgerDto> GetLedgerAsync(Guid customerId, PaginationRequest pagination)
    {
        var outstanding = await GetOutstandingAsync(customerId);
        var advance = await GetAdvanceBalanceAsync(customerId);

        var ledger = await _context.CustomerLedgers
            .Include(l => l.Customer)
            .Include(l => l.VoucherEntries)
                .ThenInclude(ve => ve.Voucher)
            .FirstOrDefaultAsync(l => l.CustomerId == customerId);

        if (ledger == null)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) throw new KeyNotFoundException("Customer not found.");

            return new CustomerLedgerDto
            {
                CustomerId = customerId,
                LedgerNo = "N/A",
                CustomerName = customer.Name,
                OpeningBalance = 0,
                OpeningBalanceType = BalanceType.Debit.ToString(),
                CurrentBalance = 0,
                OutstandingAmount = outstanding,
                AdvanceAmount = advance,
                Entries = new PagedResponse<LedgerEntryDto>()
            };
        }

        decimal runningBalance = ledger.OpeningBalanceType == BalanceType.Debit 
            ? ledger.OpeningBalance 
            : -ledger.OpeningBalance;

        var entries = ledger.VoucherEntries
            .OrderBy(e => e.Voucher.VoucherDate)
            .ThenBy(e => e.CreatedDate)
            .Select(e => 
            {
                runningBalance += e.DebitAmount;
                runningBalance -= e.CreditAmount;

                return new LedgerEntryDto
                {
                    Date = e.Voucher.VoucherDate,
                    VoucherNumber = e.Voucher.VoucherNumber,
                    VoucherType = e.Voucher.VoucherType.ToString(),
                    Narration = e.Remarks ?? e.Voucher.Narration,
                    Debit = e.DebitAmount,
                    Credit = e.CreditAmount,
                    RunningBalance = runningBalance
                };
            }).ToList();

        decimal finalBalance = runningBalance;

        if (pagination.SortDescending)
        {
            entries.Reverse();
        }

        int count = entries.Count;
        var pagedData = entries.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var pagedEntries = new PagedResponse<LedgerEntryDto>(pagedData, count, pagination.PageNumber, pagination.PageSize);

        return new CustomerLedgerDto
        {
            CustomerId = customerId,
            LedgerNo = ledger.LedgerNo,
            CustomerName = ledger.Customer.Name,
            OpeningBalance = ledger.OpeningBalance,
            OpeningBalanceType = ledger.OpeningBalanceType.ToString(),
            CurrentBalance = finalBalance,
            OutstandingAmount = outstanding,
            AdvanceAmount = advance,
            Entries = pagedEntries
        };
    }

    public async Task<decimal> GetOutstandingAsync(Guid customerId)
    {
        var invoices = await _context.Invoices
            .Where(i => i.CustomerId == customerId)
            .Select(i => new
            {
                i.GrandTotal,
                Allocated = _context.VoucherAllocations.Where(a => a.InvoiceId == i.Id).Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        return invoices.Sum(i => i.GrandTotal - i.Allocated);
    }

    public async Task<decimal> GetAdvanceBalanceAsync(Guid customerId)
    {
        var receiptCredits = await _context.VoucherEntries
            .Where(e => e.CustomerLedgerId != null && e.CustomerLedger!.CustomerId == customerId && e.Voucher.VoucherType == VoucherType.Receipt && e.CreditAmount > 0)
            .Select(e => new
            {
                e.CreditAmount,
                Allocated = e.Allocations.Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        return receiptCredits.Sum(r => r.CreditAmount - r.Allocated);
    }

    public async Task<CustomerLedgerDto> CreateLedgerAsync(Guid customerId, CreateCustomerLedgerRequest request)
    {
        var existing = await _context.CustomerLedgers.FirstOrDefaultAsync(l => l.CustomerId == customerId);
        if (existing != null)
        {
            throw new InvalidOperationException("Ledger already exists for this customer.");
        }

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null) throw new KeyNotFoundException("Customer not found.");

        var count = await _context.CustomerLedgers.CountAsync();
        var ledgerNo = $"LE-{count + 1:D3}";

        var ledger = new Domain.Entities.CustomerLedger
        {
            Id = Guid.NewGuid(),
            LedgerNo = ledgerNo,
            CustomerId = customerId,
            OpeningBalance = request.OpeningBalance,
            OpeningBalanceType = request.OpeningBalanceType,
            IsActive = true,
            CreatedDate = DateTime.Now
        };

        _context.CustomerLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        return await GetLedgerAsync(customerId, new PaginationRequest { PageSize = int.MaxValue });
    }
}
