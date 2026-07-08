using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

using SriMuniEngineering_Api.Common.Dtos;

using SriMuniEngineering_Api.Domain.Enums;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface IInvoiceStatusService
{
    Task<InvoiceStatusDto> GetStatusAsync(Guid invoiceId);
    Task<PagedResponse<InvoiceStatusDto>> GetInvoicesByStatusAsync(Guid? customerId, string? status, PaginationRequest pagination);
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

        return new InvoiceStatusDto
        {
            InvoiceId = invoice.Id,
            CustomerId = invoice.CustomerId,
            InvoiceNumber = invoice.InvoiceNo,
            InvoiceDate = invoice.Date,
            InvoiceTotal = invoice.GrandTotal,
            AllocatedAmount = allocated,
            Outstanding = outstanding,
            Status = invoice.Status.ToString()
        };
    }

    public async Task<PagedResponse<InvoiceStatusDto>> GetInvoicesByStatusAsync(Guid? customerId, string? status, PaginationRequest pagination)
    {
        var query = _context.Invoices.AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(i => i.CustomerId == customerId.Value);
        }
        
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(i => i.Status == parsedStatus);
        }

        var invoices = await query
            .Select(i => new
            {
                i.Id,
                i.CustomerId,
                i.InvoiceNo,
                i.Date,
                i.GrandTotal,
                i.Status,
                AllocatedAmount = _context.VoucherAllocations.Where(a => a.InvoiceId == i.Id).Sum(a => a.AllocatedAmount)
            })
            .ToListAsync();

        var result = invoices.Select(i => 
        {
            decimal outstanding = i.GrandTotal - i.AllocatedAmount;
            
            return new InvoiceStatusDto
            {
                InvoiceId = i.Id,
                CustomerId = i.CustomerId,
                InvoiceNumber = i.InvoiceNo,
                InvoiceDate = i.Date,
                InvoiceTotal = i.GrandTotal,
                AllocatedAmount = i.AllocatedAmount,
                Outstanding = outstanding,
                Status = i.Status.ToString()
            };
        });

        var sortedResult = result.OrderByDescending(r => r.InvoiceDate).ToList();
        
        int count = sortedResult.Count;
        var pagedData = sortedResult.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();

        return new PagedResponse<InvoiceStatusDto>(pagedData, count, pagination.PageNumber, pagination.PageSize);
    }
}
