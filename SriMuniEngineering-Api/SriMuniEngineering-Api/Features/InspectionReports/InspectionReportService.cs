using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.InspectionReports.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.InspectionReports;

public class InspectionReportService
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _storageService;
    private readonly IConfiguration _configuration;

    public InspectionReportService(AppDbContext context, SupabaseStorageService storageService, IConfiguration configuration)
    {
        _context = context;
        _storageService = storageService;
        _configuration = configuration;
    }

    public async Task<InspectionReportResponse> CreateAsync(CreateInspectionReportRequest request)
    {
        var report = new InspectionReport
        {
            Id = Guid.NewGuid(),
            InvoiceId = request.InvoiceId,
            DcLedgerId = request.DcLedgerId,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            DrawingNo = request.DrawingNo,
            Operation = request.Operation,
            DcNo = request.DcNo,
            DcDate = request.DcDate,
            DcQty = request.DcQty,
            InspectedQty = request.InspectedQty,
            IssueNo = request.IssueNo,
            BatchNo = request.BatchNo,
            ParametersJson = JsonSerializer.Serialize(request.Parameters),
            OkQty = request.OkQty,
            RejectedQty = request.RejectedQty,
            DeviationQty = request.DeviationQty,
            VendorResult = request.VendorResult,
            CieResult = request.CieResult,
            InspectedBy = request.InspectedBy,
            ApprovedBy = request.ApprovedBy,
            CreatedAt = DateTime.Now
        };

        _context.InspectionReports.Add(report);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(report.Id);
    }

    public async Task<InspectionReportResponse> GetByIdAsync(Guid id)
    {
        var report = await _context.InspectionReports
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Include(r => r.DcLedger)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Inspection report with ID {id} not found.");

        return MapToResponse(report);
    }

    public async Task<PaginatedResponse<InspectionReportResponse>> GetAllAsync(InspectionReportFilterRequest filter)
    {
        var query = _context.InspectionReports
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.DcNo))
            query = query.Where(r => r.DcNo.Contains(filter.DcNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(r => r.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(r => r.Customer.Name.Contains(filter.CustomerName));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(r => r.Product.PartNo.Contains(filter.PartNo));

        if (!string.IsNullOrWhiteSpace(filter.Operation))
            query = query.Where(r => r.Operation.Contains(filter.Operation));

        if (filter.FromDate.HasValue)
            query = query.Where(r => r.DcDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(r => r.DcDate <= filter.ToDate.Value);

        if (filter.InvoiceId.HasValue)
            query = query.Where(r => r.InvoiceId == filter.InvoiceId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(r =>
                r.DcNo.Contains(filter.Search) ||
                r.Customer.Name.Contains(filter.Search) ||
                r.Product.PartNo.Contains(filter.Search) ||
                r.Product.PartName.Contains(filter.Search) ||
                r.Operation.Contains(filter.Search));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "dcno" => isAsc ? query.OrderBy(r => r.DcNo) : query.OrderByDescending(r => r.DcNo),
            "dcdate" => isAsc ? query.OrderBy(r => r.DcDate) : query.OrderByDescending(r => r.DcDate),
            "customer" => isAsc ? query.OrderBy(r => r.Customer.Name) : query.OrderByDescending(r => r.Customer.Name),
            "partno" => isAsc ? query.OrderBy(r => r.Product.PartNo) : query.OrderByDescending(r => r.Product.PartNo),
            "operation" => isAsc ? query.OrderBy(r => r.Operation) : query.OrderByDescending(r => r.Operation),
            _ => query.OrderByDescending(r => r.CreatedAt)
        };

        // ─── Pagination ───────────────────────────────────────
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<InspectionReportResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Returns the download URL for the inspection report's PDF. If not present (or if regenerate is true), generates and uploads it.
    /// </summary>
    public async Task<string> GetPdfUrlAsync(Guid id, string baseUrl, bool regenerate = false)
    {
        var report = await _context.InspectionReports
            .Include(r => r.Customer)
            .Include(r => r.Product)
            .Include(r => r.DcLedger)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new KeyNotFoundException($"Inspection report with ID {id} not found.");

        if (!regenerate && !string.IsNullOrEmpty(report.StoredFilePath))
        {
            return $"/api/files/inspection-report/{report.Id}.pdf";
        }

        // Generate the PDF since it doesn't exist yet
        var companyProfile = _configuration.GetSection("CompanyProfile");
        var pdfBytes = InspectionReportPdfGenerator.Generate(report, companyProfile);

        // Upload to Supabase
        var fileName = $"IR-{report.Id.ToString()[..8]}.pdf";
        var storedPath = await _storageService.UploadFileAsync("inspection-reports", fileName, pdfBytes, "application/pdf");

        // Update DB
        report.StoredFilePath = storedPath;
        await _context.SaveChangesAsync();

        return $"{baseUrl}/api/files/inspection-report/{report.Id}.pdf";
    }

    private InspectionReportResponse MapToResponse(InspectionReport report)
    {
        var parameters = JsonSerializer.Deserialize<List<InspectionParameter>>(report.ParametersJson) ?? [];

        return new InspectionReportResponse
        {
            Id = report.Id,
            InvoiceId = report.InvoiceId,
            DcLedgerId = report.DcLedgerId,
            CustomerId = report.CustomerId,
            CustomerName = report.Customer.Name,
            ProductId = report.ProductId,
            PartNo = report.Product.PartNo,
            PartName = report.Product.PartName,
            DrawingNo = report.DrawingNo,
            Operation = report.Operation,
            DcNo = report.DcNo,
            DcDate = report.DcDate,
            DcQty = report.DcQty,
            InspectedQty = report.InspectedQty,
            IssueNo = report.IssueNo,
            BatchNo = report.BatchNo,
            Parameters = parameters,
            OkQty = report.OkQty,
            RejectedQty = report.RejectedQty,
            DeviationQty = report.DeviationQty,
            VendorResult = report.VendorResult,
            CieResult = report.CieResult,
            InspectedBy = report.InspectedBy,
            ApprovedBy = report.ApprovedBy,
            DownloadUrl = report.StoredFilePath != null ? $"/api/files/inspection-report/{report.Id}.pdf" : null,
            CreatedAt = report.CreatedAt
        };
    }
}
