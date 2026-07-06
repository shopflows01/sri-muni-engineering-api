using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

using SriMuniEngineering_Api.Common.Dtos;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface IAccountsDashboardService
{
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync();
    Task<PagedResponse<CustomerOutstandingDto>> GetCustomerOutstandingAsync(PaginationRequest pagination);
    Task<CustomerOutstandingDetailDto> GetCustomerOutstandingDetailAsync(Guid customerId);
}

public class AccountsDashboardService : IAccountsDashboardService
{
    private readonly AppDbContext _context;
    private readonly IInvoiceStatusService _invoiceStatusService;

    public AccountsDashboardService(AppDbContext context, IInvoiceStatusService invoiceStatusService)
    {
        _context = context;
        _invoiceStatusService = invoiceStatusService;
    }

    public async Task<InvoiceSummaryDto> GetInvoiceSummaryAsync()
    {
        var allInvoicesPaged = await _invoiceStatusService.GetInvoicesByStatusAsync(null, null, new PaginationRequest { PageSize = int.MaxValue });
        var allInvoices = allInvoicesPaged.Items.ToList();

        return new InvoiceSummaryDto
        {
            TotalInvoices = allInvoices.Count,
            PaidCount = allInvoices.Count(i => i.Status == "Paid"),
            UnpaidCount = allInvoices.Count(i => i.Status == "Unpaid"),
            PartiallyPaidCount = allInvoices.Count(i => i.Status == "PartiallyPaid"),
            TotalInvoiceAmount = allInvoices.Sum(i => i.InvoiceTotal),
            TotalOutstanding = allInvoices.Sum(i => i.Outstanding)
        };
    }

    public async Task<PagedResponse<CustomerOutstandingDto>> GetCustomerOutstandingAsync(PaginationRequest pagination)
    {
        var customers = await _context.Customers.ToListAsync();
        var allInvoicesPaged = await _invoiceStatusService.GetInvoicesByStatusAsync(null, null, new PaginationRequest { PageSize = int.MaxValue });
        var allInvoices = allInvoicesPaged.Items.ToList();

        // Calculate advance balances for all customers
        var receiptCredits = await _context.VoucherEntries
            .Include(e => e.CustomerLedger)
            .Include(e => e.Voucher)
            .Include(e => e.Allocations)
            .Where(e => e.CustomerLedgerId != null && e.Voucher.VoucherType == VoucherType.Receipt && e.CreditAmount > 0)
            .ToListAsync();

        var result = new List<CustomerOutstandingDto>();

        foreach (var customer in customers)
        {
            var customerInvoices = allInvoices.Where(i => _context.Invoices.Any(dbI => dbI.Id == i.InvoiceId && dbI.CustomerId == customer.Id)).ToList();
            
            decimal totalInvoiced = customerInvoices.Sum(i => i.InvoiceTotal);
            decimal outstanding = customerInvoices.Sum(i => i.Outstanding);
            decimal totalPaid = customerInvoices.Sum(i => i.AllocatedAmount);

            decimal advanceBalance = receiptCredits
                .Where(e => e.CustomerLedger!.CustomerId == customer.Id)
                .Sum(e => e.CreditAmount - e.Allocations.Sum(a => a.AllocatedAmount));

            if (totalInvoiced > 0 || advanceBalance > 0)
            {
                result.Add(new CustomerOutstandingDto
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.Name,
                    TotalInvoiced = totalInvoiced,
                    TotalPaid = totalPaid,
                    Outstanding = outstanding,
                    AdvanceBalance = advanceBalance
                });
            }
        }

        var sortedResult = result.OrderByDescending(r => r.Outstanding).ToList();
        
        int count = sortedResult.Count;
        var pagedData = sortedResult.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();

        return new PagedResponse<CustomerOutstandingDto>(pagedData, count, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CustomerOutstandingDetailDto> GetCustomerOutstandingDetailAsync(Guid customerId)
    {
        var invoicesPaged = await _invoiceStatusService.GetInvoicesByStatusAsync(customerId, null, new PaginationRequest { PageSize = int.MaxValue });
        return new CustomerOutstandingDetailDto
        {
            CustomerId = customerId,
            Invoices = invoicesPaged.Items.ToList()
        };
    }
}
