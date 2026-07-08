using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.StockLedger.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.StockLedger;

public class StockService
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _storageService;

    public StockService(AppDbContext context, SupabaseStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<LedgerResponse> CreateInwardAsync(InwardRequest request)
    {
        var ledger = new JobWorkLedger
        {
            Id = Guid.NewGuid(),
            DcNo = request.DcNo,
            DcDate = request.DcDate,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            InwardQty = request.InwardQty,
            OutwardQty = 0,
            RejectedQty = 0,
            Status = LedgerStatus.InProgress,
            CreatedAt = DateTime.Now
        };

        _context.JobWorkLedgers.Add(ledger);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(ledger.Id);
    }

    public async Task<LedgerResponse> UpdateOutwardAsync(Guid id, OutwardRequest request)
    {
        var ledger = await _context.JobWorkLedgers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ledger entry with ID {id} not found.");

        var newOutward = ledger.OutwardQty + request.OutwardQty;
        if (newOutward + ledger.RejectedQty > ledger.InwardQty)
            throw new InvalidOperationException(
                $"Total OutwardQty ({newOutward}) + RejectedQty ({ledger.RejectedQty}) cannot exceed InwardQty ({ledger.InwardQty}).");

        ledger.OutwardQty = newOutward;
        ledger.Status = (ledger.OutwardQty + ledger.RejectedQty == ledger.InwardQty)
            ? LedgerStatus.ReadyForInvoice
            : LedgerStatus.InProgress;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(ledger.Id);
    }

    public async Task<LedgerResponse> UpdateRejectedAsync(Guid id, RejectedRequest request)
    {
        var ledger = await _context.JobWorkLedgers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ledger entry with ID {id} not found.");

        var newRejected = ledger.RejectedQty + request.RejectedQty;
        if (ledger.OutwardQty + newRejected > ledger.InwardQty)
            throw new InvalidOperationException(
                $"OutwardQty ({ledger.OutwardQty}) + Total RejectedQty ({newRejected}) cannot exceed InwardQty ({ledger.InwardQty}).");

        ledger.RejectedQty = newRejected;
        ledger.Status = (ledger.OutwardQty + ledger.RejectedQty == ledger.InwardQty)
            ? LedgerStatus.ReadyForInvoice
            : LedgerStatus.InProgress;

        await _context.SaveChangesAsync();

        return await GetByIdAsync(ledger.Id);
    }

    public async Task<LedgerResponse> GetByIdAsync(Guid id)
    {
        var ledger = await _context.JobWorkLedgers
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == id)
            ?? throw new KeyNotFoundException($"Ledger entry with ID {id} not found.");

        return MapToResponse(ledger);
    }

    public async Task DeleteAsync(Guid id)
    {
        var ledger = await _context.JobWorkLedgers.FindAsync(id)
            ?? throw new KeyNotFoundException($"Ledger entry with ID {id} not found.");

        _context.JobWorkLedgers.Remove(ledger);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<LedgerResponse>> GetAllAsync(StockFilterRequest filter)
    {
        var query = _context.JobWorkLedgers
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.DcNo))
            query = query.Where(l => l.DcNo.Contains(filter.DcNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(l => l.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(l => l.ProductId == filter.ProductId.Value);

        if (filter.Status.HasValue)
            query = query.Where(l => l.Status == filter.Status.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(l => l.DcDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(l => l.DcDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(l => l.Customer.Name.Contains(filter.CustomerName));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(l => l.Product.PartNo.Contains(filter.PartNo));

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(l =>
                l.DcNo.Contains(filter.Search) ||
                l.Customer.Name.Contains(filter.Search) ||
                l.Product.PartNo.Contains(filter.Search) ||
                l.Product.PartName.Contains(filter.Search));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "dcno" => isAsc ? query.OrderBy(l => l.DcNo) : query.OrderByDescending(l => l.DcNo),
            "dcdate" => isAsc ? query.OrderBy(l => l.DcDate) : query.OrderByDescending(l => l.DcDate),
            "customer" => isAsc ? query.OrderBy(l => l.Customer.Name) : query.OrderByDescending(l => l.Customer.Name),
            "partno" => isAsc ? query.OrderBy(l => l.Product.PartNo) : query.OrderByDescending(l => l.Product.PartNo),
            "inwardqty" => isAsc ? query.OrderBy(l => l.InwardQty) : query.OrderByDescending(l => l.InwardQty),
            "status" => isAsc ? query.OrderBy(l => l.Status) : query.OrderByDescending(l => l.Status),
            _ => query.OrderByDescending(l => l.CreatedAt)
        };

        // ─── Pagination ───────────────────────────────────────
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<LedgerResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<string> ExportToExcelAsync(StockExportQuery query, string baseUrl)
    {
        var fromDate = query.FromDate ?? (query.Period == "weekly"
            ? DateTime.Now.AddDays(-7)
            : DateTime.Now.AddMonths(-1));
        var toDate = query.ToDate ?? DateTime.Now;

        var ledgers = await _context.JobWorkLedgers
            .Include(l => l.Customer)
            .Include(l => l.Product)
            .Where(l => l.DcDate >= fromDate && l.DcDate <= toDate)
            .OrderByDescending(l => l.DcDate)
            .ToListAsync();

        var stream = new MemoryStream();

        // Workbook must be fully disposed before reading the stream.
        // ClosedXML writes the ZIP end-of-central-directory record only on Dispose(),
        // so calling ToArray() while the workbook is still alive produces a truncated file.
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Stock Report");

            // Header row
            worksheet.Cell(1, 1).Value = "DC No";
            worksheet.Cell(1, 2).Value = "DC Date";
            worksheet.Cell(1, 3).Value = "Customer";
            worksheet.Cell(1, 4).Value = "Part No";
            worksheet.Cell(1, 5).Value = "Part Name";
            worksheet.Cell(1, 6).Value = "Inward Qty";
            worksheet.Cell(1, 7).Value = "Outward Qty";
            worksheet.Cell(1, 8).Value = "Rejected Qty";
            worksheet.Cell(1, 9).Value = "Status";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data rows
            for (int i = 0; i < ledgers.Count; i++)
            {
                var row = i + 2;
                var l = ledgers[i];
                worksheet.Cell(row, 1).Value = l.DcNo;
                worksheet.Cell(row, 2).Value = l.DcDate.ToString("dd-MM-yyyy");
                worksheet.Cell(row, 3).Value = l.Customer.Name;
                worksheet.Cell(row, 4).Value = l.Product.PartNo;
                worksheet.Cell(row, 5).Value = l.Product.PartName;
                worksheet.Cell(row, 6).Value = l.InwardQty;
                worksheet.Cell(row, 7).Value = l.OutwardQty;
                worksheet.Cell(row, 8).Value = l.RejectedQty;
                worksheet.Cell(row, 9).Value = l.Status.ToString();
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        } // Dispose flushes ZIP central directory into the stream

        var bytes = stream.ToArray();

        // Upload to Supabase
        var fileName = $"StockReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        await _storageService.UploadFileAsync(
            "reports", fileName, bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // Return clean proxy URL (no Supabase metadata exposed)
        return $"{baseUrl}/api/files/report/{Uri.EscapeDataString(fileName)}";
    }

    private static LedgerResponse MapToResponse(JobWorkLedger ledger) => new()
    {
        Id = ledger.Id,
        DcNo = ledger.DcNo,
        DcDate = ledger.DcDate,
        CustomerId = ledger.CustomerId,
        CustomerName = ledger.Customer.Name,
        ProductId = ledger.ProductId,
        PartNo = ledger.Product.PartNo,
        PartName = ledger.Product.PartName,
        InwardQty = ledger.InwardQty,
        OutwardQty = ledger.OutwardQty,
        RejectedQty = ledger.RejectedQty,
        Status = ledger.Status,
        CreatedAt = ledger.CreatedAt
    };
}
