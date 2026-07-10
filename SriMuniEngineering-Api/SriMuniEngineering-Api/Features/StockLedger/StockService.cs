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

    public async Task<JobWorkDCResponse> CreateDCAsync(CreateJobWorkDCRequest request)
    {
        var dc = new JobWorkDC
        {
            Id = Guid.NewGuid(),
            DcNo = request.DcNo,
            DcDate = request.DcDate,
            CustomerId = request.CustomerId,
            Remarks = request.Remarks,
            Status = LedgerStatus.InProgress,
            CreatedDate = DateTime.Now
        };

        foreach (var reqItem in request.Items)
        {
            var item = new JobWorkDCItem
            {
                Id = Guid.NewGuid(),
                DcId = dc.Id,
                ProductId = reqItem.ProductId,
                QtySent = reqItem.QtySent,
                Rate = reqItem.Rate,
                GstPercent = reqItem.GstPercent,
                Remarks = reqItem.Remarks
            };
            
            // Add inward transaction automatically for the qty sent
            item.Transactions.Add(new JobWorkTransaction
            {
                Id = Guid.NewGuid(),
                DcItemId = item.Id,
                TransactionDate = request.DcDate,
                TransactionType = TransactionType.Inward,
                Quantity = reqItem.QtySent,
                Remarks = "Initial Inward"
            });

            dc.Items.Add(item);
        }

        _context.JobWorkDCs.Add(dc);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(dc.Id);
    }

    public async Task<JobWorkDCResponse> AddTransactionAsync(Guid dcItemId, TransactionRequest request)
    {
        var item = await _context.JobWorkDCItems
            .Include(i => i.JobWorkDC)
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.Id == dcItemId)
            ?? throw new KeyNotFoundException($"DC Item with ID {dcItemId} not found.");

        var newTransaction = new JobWorkTransaction
        {
            Id = Guid.NewGuid(),
            DcItemId = dcItemId,
            TransactionDate = request.TransactionDate,
            TransactionType = request.TransactionType,
            Quantity = request.Quantity,
            ReferenceNo = request.ReferenceNo,
            Remarks = request.Remarks
        };

        item.Transactions.Add(newTransaction);
        
        // Update DC Status if all items are fully processed (inward == outward + rejected)
        await _context.SaveChangesAsync();
        
        return await GetByIdAsync(item.DcId);
    }

    public async Task<JobWorkDCResponse> GetByIdAsync(Guid id)
    {
        var dc = await _context.JobWorkDCs
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .Include(d => d.Items)
                .ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"DC entry with ID {id} not found.");

        return MapToResponse(dc);
    }

    public async Task DeleteAsync(Guid id)
    {
        var dc = await _context.JobWorkDCs.FindAsync(id)
            ?? throw new KeyNotFoundException($"DC entry with ID {id} not found.");

        _context.JobWorkDCs.Remove(dc);
        await _context.SaveChangesAsync();
    }

    public async Task<PaginatedResponse<JobWorkDCResponse>> GetAllAsync(StockFilterRequest filter)
    {
        var query = _context.JobWorkDCs
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .Include(d => d.Items)
                .ThenInclude(i => i.Transactions)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.DcNo))
            query = query.Where(d => d.DcNo.Contains(filter.DcNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(d => d.CustomerId == filter.CustomerId.Value);

        if (filter.Status.HasValue)
            query = query.Where(d => d.Status == filter.Status.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(d => d.DcDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(d => d.DcDate <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(d => d.Customer.Name.Contains(filter.CustomerName));
            
        if (filter.ProductId.HasValue)
            query = query.Where(d => d.Items.Any(i => i.ProductId == filter.ProductId.Value));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(d => d.Items.Any(i => i.Product.PartNo.Contains(filter.PartNo)));

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(d =>
                d.DcNo.Contains(filter.Search) ||
                d.Customer.Name.Contains(filter.Search) ||
                d.Items.Any(i => i.Product.PartNo.Contains(filter.Search)) ||
                d.Items.Any(i => i.Product.PartName.Contains(filter.Search)));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "dcno" => isAsc ? query.OrderBy(d => d.DcNo) : query.OrderByDescending(d => d.DcNo),
            "dcdate" => isAsc ? query.OrderBy(d => d.DcDate) : query.OrderByDescending(d => d.DcDate),
            "customer" => isAsc ? query.OrderBy(d => d.Customer.Name) : query.OrderByDescending(d => d.Customer.Name),
            "status" => isAsc ? query.OrderBy(d => d.Status) : query.OrderByDescending(d => d.Status),
            _ => query.OrderByDescending(d => d.CreatedDate)
        };

        // ─── Pagination ───────────────────────────────────────
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<JobWorkDCResponse>
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

        var dcs = await _context.JobWorkDCs
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .Include(d => d.Items)
                .ThenInclude(i => i.Transactions)
            .Where(d => d.DcDate >= fromDate && d.DcDate <= toDate)
            .OrderByDescending(d => d.DcDate)
            .ToListAsync();

        var stream = new MemoryStream();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Stock Report");

            // Header row
            worksheet.Cell(1, 1).Value = "DC No";
            worksheet.Cell(1, 2).Value = "DC Date";
            worksheet.Cell(1, 3).Value = "Customer";
            worksheet.Cell(1, 4).Value = "Part No";
            worksheet.Cell(1, 5).Value = "Part Name";
            worksheet.Cell(1, 6).Value = "Qty Sent";
            worksheet.Cell(1, 7).Value = "Inward Qty";
            worksheet.Cell(1, 8).Value = "Outward Qty";
            worksheet.Cell(1, 9).Value = "Rejected Qty";
            worksheet.Cell(1, 10).Value = "Status";

            // Style header
            var headerRange = worksheet.Range(1, 1, 1, 10);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Data rows (one row per item)
            var row = 2;
            foreach (var dc in dcs)
            {
                foreach (var item in dc.Items)
                {
                    var inward = item.Transactions.Where(t => t.TransactionType == TransactionType.Inward).Sum(t => t.Quantity);
                    var outward = item.Transactions.Where(t => t.TransactionType == TransactionType.Outward).Sum(t => t.Quantity);
                    var rejected = item.Transactions.Where(t => t.TransactionType == TransactionType.Rejected).Sum(t => t.Quantity);

                    worksheet.Cell(row, 1).Value = dc.DcNo;
                    worksheet.Cell(row, 2).Value = dc.DcDate.ToString("dd-MM-yyyy");
                    worksheet.Cell(row, 3).Value = dc.Customer.Name;
                    worksheet.Cell(row, 4).Value = item.Product.PartNo;
                    worksheet.Cell(row, 5).Value = item.Product.PartName;
                    worksheet.Cell(row, 6).Value = item.QtySent;
                    worksheet.Cell(row, 7).Value = inward;
                    worksheet.Cell(row, 8).Value = outward;
                    worksheet.Cell(row, 9).Value = rejected;
                    worksheet.Cell(row, 10).Value = dc.Status.ToString();
                    row++;
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(stream);
        }

        var bytes = stream.ToArray();

        var fileName = $"StockReport_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        await _storageService.UploadFileAsync(
            "reports", fileName, bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        return $"{baseUrl}/api/files/report/{Uri.EscapeDataString(fileName)}";
    }

    public async Task<JobWorkDCResponse> UpdateDCAsync(Guid id, UpdateJobWorkDCRequest request)
    {
        var dc = await _context.JobWorkDCs
            .Include(d => d.Items)
                .ThenInclude(i => i.Transactions)
            .FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new KeyNotFoundException($"DC entry with ID {id} not found.");

        dc.DcNo = request.DcNo;
        dc.DcDate = request.DcDate;
        dc.CustomerId = request.CustomerId;
        dc.Remarks = request.Remarks;

        // Keep track of requested item IDs to find deleted items
        var requestedItemIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToList();
        
        // Remove deleted items
        var itemsToRemove = dc.Items.Where(i => !requestedItemIds.Contains(i.Id)).ToList();
        foreach (var itemToRemove in itemsToRemove)
        {
            _context.JobWorkDCItems.Remove(itemToRemove);
        }

        // Add or update items
        foreach (var reqItem in request.Items)
        {
            if (reqItem.Id.HasValue && reqItem.Id.Value != Guid.Empty)
            {
                var existingItem = dc.Items.FirstOrDefault(i => i.Id == reqItem.Id.Value);
                if (existingItem != null)
                {
                    existingItem.ProductId = reqItem.ProductId;
                    existingItem.QtySent = reqItem.QtySent;
                    existingItem.Rate = reqItem.Rate;
                    existingItem.GstPercent = reqItem.GstPercent;
                    existingItem.Remarks = reqItem.Remarks;
                    
                    // Update initial inward transaction if QtySent changed
                    var initialInward = existingItem.Transactions.FirstOrDefault(t => t.TransactionType == TransactionType.Inward && t.Remarks == "Initial Inward");
                    if (initialInward != null)
                    {
                        initialInward.Quantity = reqItem.QtySent;
                    }
                }
            }
            else
            {
                var newItem = new JobWorkDCItem
                {
                    Id = Guid.NewGuid(),
                    DcId = dc.Id,
                    ProductId = reqItem.ProductId,
                    QtySent = reqItem.QtySent,
                    Rate = reqItem.Rate,
                    GstPercent = reqItem.GstPercent,
                    Remarks = reqItem.Remarks
                };

                newItem.Transactions.Add(new JobWorkTransaction
                {
                    Id = Guid.NewGuid(),
                    DcItemId = newItem.Id,
                    TransactionDate = request.DcDate,
                    TransactionType = TransactionType.Inward,
                    Quantity = reqItem.QtySent,
                    Remarks = "Initial Inward"
                });

                dc.Items.Add(newItem);
            }
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(dc.Id);
    }

    public async Task<PaginatedResponse<TransactionHistoryResponse>> GetTransactionsAsync(TransactionFilterRequest filter)
    {
        var query = _context.JobWorkTransactions
            .Include(t => t.DcItem)
                .ThenInclude(i => i.JobWorkDC)
                    .ThenInclude(d => d.Customer)
            .Include(t => t.DcItem)
                .ThenInclude(i => i.Product)
            .AsQueryable();

        if (filter.FromDate.HasValue)
            query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);

        if (filter.CustomerId.HasValue)
            query = query.Where(t => t.DcItem.JobWorkDC.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(t => t.DcItem.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.TransactionType) && Enum.TryParse<TransactionType>(filter.TransactionType, true, out var typeEnum))
            query = query.Where(t => t.TransactionType == typeEnum);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(t => 
                t.DcItem.JobWorkDC.DcNo.Contains(filter.Search) ||
                t.DcItem.Product.PartNo.Contains(filter.Search) ||
                t.DcItem.JobWorkDC.Customer.Name.Contains(filter.Search));

        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "date" => isAsc ? query.OrderBy(t => t.TransactionDate) : query.OrderByDescending(t => t.TransactionDate),
            "dcno" => isAsc ? query.OrderBy(t => t.DcItem.JobWorkDC.DcNo) : query.OrderByDescending(t => t.DcItem.JobWorkDC.DcNo),
            _ => query.OrderByDescending(t => t.TransactionDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TransactionHistoryResponse
            {
                TransactionId = t.Id,
                DcItemId = t.DcItemId,
                DcId = t.DcItem.DcId,
                DcNo = t.DcItem.JobWorkDC.DcNo,
                DcDate = t.DcItem.JobWorkDC.DcDate,
                CustomerName = t.DcItem.JobWorkDC.Customer.Name,
                PartNo = t.DcItem.Product.PartNo,
                PartName = t.DcItem.Product.PartName,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType.ToString(),
                Quantity = t.Quantity,
                ReferenceNo = t.ReferenceNo,
                Remarks = t.Remarks
            })
            .ToListAsync();

        return new PaginatedResponse<TransactionHistoryResponse>
        {
            Items = items,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    private static JobWorkDCResponse MapToResponse(JobWorkDC dc) => new()
    {
        Id = dc.Id,
        DcNo = dc.DcNo,
        DcDate = dc.DcDate,
        CustomerId = dc.CustomerId,
        CustomerName = dc.Customer.Name,
        Remarks = dc.Remarks,
        Status = dc.Status,
        CreatedAt = dc.CreatedDate,
        Items = dc.Items.Select(i => new JobWorkDCItemResponse
        {
            Id = i.Id,
            ProductId = i.ProductId,
            PartNo = i.Product.PartNo,
            PartName = i.Product.PartName,
            QtySent = i.QtySent,
            Rate = i.Rate,
            GstPercent = i.GstPercent,
            Remarks = i.Remarks,
            InwardQty = i.Transactions.Where(t => t.TransactionType == TransactionType.Inward).Sum(t => t.Quantity),
            OutwardQty = i.Transactions.Where(t => t.TransactionType == TransactionType.Outward).Sum(t => t.Quantity),
            RejectedQty = i.Transactions.Where(t => t.TransactionType == TransactionType.Rejected).Sum(t => t.Quantity),
            Transactions = i.Transactions.OrderByDescending(t => t.TransactionDate).Select(t => new JobWorkTransactionResponse
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                TransactionType = t.TransactionType,
                Quantity = t.Quantity,
                ReferenceNo = t.ReferenceNo,
                Remarks = t.Remarks
            }).ToList()
        }).ToList()
    };
}
