using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface IInvoiceStatusService
{
    Task<InvoiceStatusDto> GetStatusAsync(Guid invoiceId);
    Task<List<InvoiceStatusDto>> GetInvoicesByStatusAsync(Guid? customerId, string? status);
}

public class InvoiceStatusService : IInvoiceStatusService
{
    private readonly AppDbContext _context;

    public InvoiceStatusService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<InvoiceStatusDto> GetStatusAsync(Guid invoiceId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found.");

        var allocated = await _context.VoucherAllocations
            .Where(a => a.InvoiceId == invoiceId)
            .SumAsync(a => a.AllocatedAmount);

        decimal outstanding = invoice.GrandTotal - allocated;
        string status = outstanding == invoice.GrandTotal ? "Unpaid" : (outstanding == 0 ? "Paid" : "PartiallyPaid");

        return new InvoiceStatusDto
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.InvoiceNo,
            InvoiceDate = invoice.Date,
            InvoiceTotal = invoice.GrandTotal,
            AllocatedAmount = allocated,
            Outstanding = outstanding,
            Status = status
        };
    }

    public async Task<List<InvoiceStatusDto>> GetInvoicesByStatusAsync(Guid? customerId, string? status)
    {
        var query = _context.Invoices.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }

        var invoices = await query
            .Select(i => new
            {
                i.Id,
                i.InvoiceNo,
                i.Date,
                i.GrandTotal,
                AllocatedAmount = _context.VoucherAllocations.Where(a => a.InvoiceId == i.Id).Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        var result = invoices.Select(i => 
        {
            decimal outstanding = i.GrandTotal - i.AllocatedAmount;
            string calcStatus = outstanding == i.GrandTotal ? "Unpaid" : (outstanding == 0 ? "Paid" : "PartiallyPaid");
            
            return new InvoiceStatusDto
            {
                InvoiceId = i.Id,
                InvoiceNumber = i.InvoiceNo,
                InvoiceDate = i.Date,
                InvoiceTotal = i.GrandTotal,
                AllocatedAmount = i.AllocatedAmount,
                Outstanding = outstanding,
                Status = calcStatus
            };
        });

        if (!string.IsNullOrEmpty(status))
        {
            result = result.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        return result.OrderByDescending(r => r.InvoiceDate).ToList();
    }
}
