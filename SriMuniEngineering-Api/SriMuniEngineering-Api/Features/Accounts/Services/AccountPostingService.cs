using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public class AccountPostingService
{
    private readonly AppDbContext _context;

    public AccountPostingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task PostSalesInvoiceAsync(Guid invoiceId, Guid customerId, decimal amount, string invoiceNo)
    {
        var ledger = await GetOrCreateCustomerLedgerAsync(customerId);

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            VoucherNumber = $"SV-{invoiceNo}",
            VoucherType = VoucherType.Sales,
            VoucherDate = DateTime.Now,
            ReferenceNumber = invoiceNo,
            Narration = $"Sales Invoice {invoiceNo}",
            Status = VoucherStatus.Posted,
            CreatedBy = "System",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        // Entry 1: Debit Customer Ledger
        var debitEntry = new VoucherEntry
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            CustomerLedgerId = ledger.Id,
            SystemAccount = null,
            DebitAmount = amount,
            CreditAmount = 0,
            Remarks = $"Invoice {invoiceNo} Auto Debit"
        };

        // Entry 2: Credit Sales (System account, CustomerLedgerId = null)
        var creditEntry = new VoucherEntry
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            CustomerLedgerId = null,
            SystemAccount = SystemAccountType.Sales,
            DebitAmount = 0,
            CreditAmount = amount,
            Remarks = $"Invoice {invoiceNo} Sales Credit"
        };

        voucher.Entries.Add(debitEntry);
        voucher.Entries.Add(creditEntry);

        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
    }

    public async Task<Voucher> PostReceiptAsync(Guid customerId, decimal amount, DateTime receiptDate, string? referenceNumber, string? narration, List<AllocationDto> allocations)
    {
        var ledger = await GetOrCreateCustomerLedgerAsync(customerId);

        // Generate Voucher Number
        var today = DateTime.Now.Date;
        var count = await _context.Vouchers.CountAsync(v => v.VoucherType == VoucherType.Receipt && v.VoucherDate.Date == today);
        var voucherNo = $"RV-{today:yyyyMMdd}-{(count + 1):D3}";

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            VoucherNumber = voucherNo,
            VoucherType = VoucherType.Receipt,
            VoucherDate = receiptDate,
            ReferenceNumber = referenceNumber,
            Narration = narration ?? "Payment Received",
            Status = VoucherStatus.Posted,
            CreatedBy = "System",
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };

        // Entry 1: Credit Customer Ledger
        var creditEntry = new VoucherEntry
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            CustomerLedgerId = ledger.Id,
            SystemAccount = null,
            DebitAmount = 0,
            CreditAmount = amount,
            Remarks = "Payment Received"
        };

        // Entry 2: Debit Bank/Cash (System account, CustomerLedgerId = null)
        var debitEntry = new VoucherEntry
        {
            Id = Guid.NewGuid(),
            VoucherId = voucher.Id,
            CustomerLedgerId = null,
            SystemAccount = SystemAccountType.Bank, // Using Bank as default for receipts
            DebitAmount = amount,
            CreditAmount = 0,
            Remarks = "Bank/Cash Receipt"
        };

        // Handle allocations
        if (allocations != null && allocations.Any())
        {
            decimal totalAllocated = allocations.Sum(a => a.Amount);
            if (totalAllocated > amount)
            {
                throw new InvalidOperationException("Total allocated amount cannot exceed receipt amount.");
            }

            foreach (var alloc in allocations)
            {
                if (alloc.Amount <= 0)
                {
                    throw new InvalidOperationException("Allocation amount must be greater than zero.");
                }

                var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
                if (invoice != null)
                {
                    var previouslyAllocated = await _context.VoucherAllocations
                        .Where(a => a.InvoiceId == invoice.Id)
                        .SumAsync(a => a.AllocatedAmount);
                    
                    var newTotalAllocated = previouslyAllocated + alloc.Amount;
                    if (newTotalAllocated >= invoice.GrandTotal)
                    {
                        invoice.Status = "Paid";
                    }
                    else if (newTotalAllocated > 0)
                    {
                        invoice.Status = "PartiallyPaid";
                    }
                }

                creditEntry.Allocations.Add(new VoucherAllocation
                {
                    Id = Guid.NewGuid(),
                    VoucherEntryId = creditEntry.Id,
                    InvoiceId = alloc.InvoiceId,
                    AllocatedAmount = alloc.Amount,
                    AllocationDate = receiptDate,
                    Remarks = $"Allocated to Invoice"
                });
            }
        }

        voucher.Entries.Add(creditEntry);
        voucher.Entries.Add(debitEntry);

        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();

        return voucher;
    }

    private async Task<CustomerLedger> GetOrCreateCustomerLedgerAsync(Guid customerId)
    {
        var ledger = await _context.CustomerLedgers.FirstOrDefaultAsync(l => l.CustomerId == customerId);
        if (ledger == null)
        {
            var count = await _context.CustomerLedgers.CountAsync();
            var ledgerNo = $"L-{count + 1:D3}";

            ledger = new CustomerLedger
            {
                Id = Guid.NewGuid(),
                LedgerNo = ledgerNo,
                CustomerId = customerId,
                OpeningBalance = 0,
                OpeningBalanceType = BalanceType.Debit,
                IsActive = true
            };
            _context.CustomerLedgers.Add(ledger);
            await _context.SaveChangesAsync();
        }
        return ledger;
    }
}
