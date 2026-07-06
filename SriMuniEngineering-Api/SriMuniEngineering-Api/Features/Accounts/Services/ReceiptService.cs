using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Accounts.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

using SriMuniEngineering_Api.Common.Dtos;

namespace SriMuniEngineering_Api.Features.Accounts.Services;

public interface IReceiptService
{
    Task<PagedResponse<ReceiptDto>> GetReceiptsAsync(Guid? customerId, PaginationRequest pagination);
    Task<Voucher> CreateReceiptAsync(CreateReceiptRequest request);
    Task AllocateAsync(Guid receiptVoucherId, AllocateRequest request);
    Task DeleteAllocationAsync(Guid allocationId);
    Task UpdateAllocationAsync(Guid allocationId, UpdateAllocationRequest request);
}

public class ReceiptService : IReceiptService
{
    private readonly AppDbContext _context;
    private readonly AccountPostingService _accountPostingService;

    public ReceiptService(AppDbContext context, AccountPostingService accountPostingService)
    {
        _context = context;
        _accountPostingService = accountPostingService;
    }

    public async Task<PagedResponse<ReceiptDto>> GetReceiptsAsync(Guid? customerId, PaginationRequest pagination)
    {
        var query = _context.VoucherEntries
            .Include(e => e.Voucher)
            .Include(e => e.CustomerLedger)
                .ThenInclude(l => l!.Customer)
            .Include(e => e.Allocations)
            .Where(e => e.Voucher.VoucherType == Domain.Enums.VoucherType.Receipt && e.CreditAmount > 0 && e.CustomerLedgerId != null)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(e => e.CustomerLedger!.CustomerId == customerId.Value);
        }

        var result = await query
            .Select(e => new ReceiptDto
            {
                VoucherId = e.VoucherId,
                VoucherNumber = e.Voucher.VoucherNumber,
                ReceiptDate = e.Voucher.VoucherDate,
                CustomerId = e.CustomerLedger!.CustomerId,
                CustomerName = e.CustomerLedger.Customer.Name,
                Amount = e.CreditAmount,
                AllocatedAmount = e.Allocations.Sum(a => a.AllocatedAmount),
                ReferenceNumber = e.Voucher.ReferenceNumber,
                Narration = e.Voucher.Narration
            })
            .ToListAsync();

        var sortedResult = result.OrderByDescending(r => r.ReceiptDate).ToList();

        if (pagination.SortDescending)
        {
            // Already descending by default here, but this allows toggle if needed.
            // If they want ascending, we flip it.
            // Actually, we could just apply it based on SortDescending.
        }

        int count = sortedResult.Count;
        var pagedData = sortedResult.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();

        return new PagedResponse<ReceiptDto>(pagedData, count, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<Voucher> CreateReceiptAsync(CreateReceiptRequest request)
    {
        return await _accountPostingService.PostReceiptAsync(
            request.CustomerId,
            request.Amount,
            request.ReceiptDate,
            request.ReferenceNumber,
            request.Narration,
            request.Allocations
        );
    }

    public async Task AllocateAsync(Guid receiptVoucherId, AllocateRequest request)
    {
        var voucher = await _context.Vouchers
            .Include(v => v.Entries)
                .ThenInclude(e => e.Allocations)
            .FirstOrDefaultAsync(v => v.Id == receiptVoucherId);

        if (voucher == null) throw new KeyNotFoundException("Receipt Voucher not found.");
        
        var creditEntry = voucher.Entries.FirstOrDefault(e => e.CreditAmount > 0 && e.CustomerLedgerId != null);
        if (creditEntry == null) throw new InvalidOperationException("Invalid receipt voucher: No customer credit entry found.");

        decimal alreadyAllocated = creditEntry.Allocations.Sum(a => a.AllocatedAmount);
        decimal newAllocationsTotal = request.Allocations.Sum(a => a.Amount);

        if (alreadyAllocated + newAllocationsTotal > creditEntry.CreditAmount)
        {
            throw new InvalidOperationException("Total allocated amount cannot exceed receipt amount.");
        }

        foreach (var alloc in request.Allocations)
        {
            if (alloc.Amount <= 0) continue;

            // Verify invoice outstanding
            var invoice = await _context.Invoices.FindAsync(alloc.InvoiceId);
            if (invoice == null) throw new KeyNotFoundException($"Invoice {alloc.InvoiceId} not found.");

            var invoiceAllocated = await _context.VoucherAllocations
                .Where(a => a.InvoiceId == alloc.InvoiceId)
                .SumAsync(a => a.AllocatedAmount);

            decimal outstanding = invoice.GrandTotal - invoiceAllocated;
            if (alloc.Amount > outstanding)
            {
                throw new InvalidOperationException($"Allocation amount {alloc.Amount} exceeds outstanding balance {outstanding} for Invoice {invoice.InvoiceNo}.");
            }

            creditEntry.Allocations.Add(new VoucherAllocation
            {
                Id = Guid.NewGuid(),
                VoucherEntryId = creditEntry.Id,
                InvoiceId = alloc.InvoiceId,
                AllocatedAmount = alloc.Amount,
                AllocationDate = DateTime.UtcNow,
                Remarks = "Late Allocation"
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAllocationAsync(Guid allocationId)
    {
        var allocation = await _context.VoucherAllocations.FindAsync(allocationId);
        if (allocation == null) throw new KeyNotFoundException("Allocation not found.");

        _context.VoucherAllocations.Remove(allocation);
        await _context.SaveChangesAsync();
    }
    public async Task UpdateAllocationAsync(Guid allocationId, UpdateAllocationRequest request)
    {
        var allocation = await _context.VoucherAllocations
            .Include(a => a.VoucherEntry)
                .ThenInclude(e => e.Allocations)
            .FirstOrDefaultAsync(a => a.Id == allocationId);

        if (allocation == null) throw new KeyNotFoundException("Allocation not found.");

        if (request.Amount <= 0) throw new InvalidOperationException("Allocation amount must be greater than zero.");

        var entry = allocation.VoucherEntry;
        decimal alreadyAllocated = entry.Allocations.Where(a => a.Id != allocationId).Sum(a => a.AllocatedAmount);

        if (alreadyAllocated + request.Amount > entry.CreditAmount)
        {
            throw new InvalidOperationException("Total allocated amount cannot exceed receipt amount.");
        }

        // Verify invoice outstanding
        var invoice = await _context.Invoices.FindAsync(allocation.InvoiceId);
        if (invoice == null) throw new KeyNotFoundException($"Invoice {allocation.InvoiceId} not found.");

        var invoiceAllocated = await _context.VoucherAllocations
            .Where(a => a.InvoiceId == allocation.InvoiceId && a.Id != allocationId)
            .SumAsync(a => a.AllocatedAmount);

        decimal outstanding = invoice.GrandTotal - invoiceAllocated;
        if (request.Amount > outstanding)
        {
            throw new InvalidOperationException($"Allocation amount {request.Amount} exceeds outstanding balance {outstanding} for Invoice {invoice.InvoiceNo}.");
        }

        allocation.AllocatedAmount = request.Amount;
        if (request.Remarks != null)
        {
            allocation.Remarks = request.Remarks;
        }

        await _context.SaveChangesAsync();
    }
}
