using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Quotations.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using SriMuniEngineering_Api.Infrastructure.Storage;

namespace SriMuniEngineering_Api.Features.Quotations;

public class QuotationService
{
    private readonly AppDbContext _context;
    private readonly SupabaseStorageService _storageService;
    private readonly IConfiguration _configuration;

    public QuotationService(AppDbContext context, SupabaseStorageService storageService, IConfiguration configuration)
    {
        _context = context;
        _storageService = storageService;
        _configuration = configuration;
    }

    public async Task<QuotationResponse> CreateAsync(CreateQuotationRequest request)
    {
        var processCostTotal = request.Operations.Sum(o => o.CostPerPart);

        var quotation = new Quotation
        {
            Id = Guid.NewGuid(),
            QuotationNo = request.QuotationNo,
            Date = request.Date,
            CustomerId = request.CustomerId,
            ProductId = request.ProductId,
            Model = request.Model,
            NumberOff = request.NumberOff,
            OperationsJson = JsonSerializer.Serialize(request.Operations),
            OtherCostsJson = JsonSerializer.Serialize(request.OtherCosts),
            ProcessCostTotal = processCostTotal,
            EstimatedCostPerPart = request.EstimatedCostPerPart,
            GstRate = request.GstRate,
            CreatedAt = DateTime.UtcNow
        };

        _context.Quotations.Add(quotation);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(quotation.Id);
    }

    public async Task<QuotationResponse> GetByIdAsync(Guid id)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new KeyNotFoundException($"Quotation with ID {id} not found.");

        return MapToResponse(quotation);
    }

    public async Task<PaginatedResponse<QuotationResponse>> GetAllAsync(QuotationFilterRequest filter)
    {
        var query = _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Product)
            .AsQueryable();

        // ─── Filters ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(filter.QuotationNo))
            query = query.Where(q => q.QuotationNo.Contains(filter.QuotationNo));

        if (filter.CustomerId.HasValue)
            query = query.Where(q => q.CustomerId == filter.CustomerId.Value);

        if (filter.ProductId.HasValue)
            query = query.Where(q => q.ProductId == filter.ProductId.Value);

        if (!string.IsNullOrWhiteSpace(filter.CustomerName))
            query = query.Where(q => q.Customer.Name.Contains(filter.CustomerName));

        if (!string.IsNullOrWhiteSpace(filter.PartNo))
            query = query.Where(q => q.Product.PartNo.Contains(filter.PartNo));

        if (filter.FromDate.HasValue)
            query = query.Where(q => q.Date >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(q => q.Date <= filter.ToDate.Value);

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(q =>
                q.QuotationNo.Contains(filter.Search) ||
                q.Customer.Name.Contains(filter.Search) ||
                q.Product.PartNo.Contains(filter.Search) ||
                q.Product.PartName.Contains(filter.Search));

        // ─── Sorting ──────────────────────────────────────────
        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "quotationno" => isAsc ? query.OrderBy(q => q.QuotationNo) : query.OrderByDescending(q => q.QuotationNo),
            "date" => isAsc ? query.OrderBy(q => q.Date) : query.OrderByDescending(q => q.Date),
            "customer" => isAsc ? query.OrderBy(q => q.Customer.Name) : query.OrderByDescending(q => q.Customer.Name),
            "partno" => isAsc ? query.OrderBy(q => q.Product.PartNo) : query.OrderByDescending(q => q.Product.PartNo),
            "cost" => isAsc ? query.OrderBy(q => q.EstimatedCostPerPart) : query.OrderByDescending(q => q.EstimatedCostPerPart),
            _ => query.OrderByDescending(q => q.CreatedAt)
        };

        // ─── Pagination ───────────────────────────────────────
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<QuotationResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Returns the download URL for the quotation's PDF. If not present (or if regenerate is true), generates and uploads it.
    /// </summary>
    public async Task<string> GetPdfUrlAsync(Guid id, string baseUrl, bool regenerate = false)
    {
        var quotation = await _context.Quotations
            .Include(q => q.Customer)
            .Include(q => q.Product)
            .FirstOrDefaultAsync(q => q.Id == id)
            ?? throw new KeyNotFoundException($"Quotation with ID {id} not found.");

        if (!regenerate && !string.IsNullOrEmpty(quotation.StoredFilePath))
        {
            return $"/api/files/quotation/{Uri.EscapeDataString(quotation.QuotationNo)}.pdf";
        }

        // Generate the PDF since it doesn't exist yet
        var companyProfile = _configuration.GetSection("CompanyProfile");
        var pdfBytes = QuotationPdfGenerator.Generate(quotation, companyProfile);

        // Upload to Supabase
        var fileName = $"{quotation.QuotationNo.Replace("/", "-")}.pdf";
        var storedPath = await _storageService.UploadFileAsync("quotations", fileName, pdfBytes, "application/pdf");

        // Update DB
        quotation.StoredFilePath = storedPath;
        await _context.SaveChangesAsync();

        return $"{baseUrl}/api/files/quotation/{Uri.EscapeDataString(quotation.QuotationNo)}.pdf";
    }

    private QuotationResponse MapToResponse(Quotation q)
    {
        var operations = JsonSerializer.Deserialize<List<OperationItem>>(q.OperationsJson) ?? [];
        var otherCosts = JsonSerializer.Deserialize<OtherCosts>(q.OtherCostsJson) ?? new();

        return new QuotationResponse
        {
            Id = q.Id,
            QuotationNo = q.QuotationNo,
            Date = q.Date,
            CustomerId = q.CustomerId,
            CustomerName = q.Customer.Name,
            ProductId = q.ProductId,
            PartNo = q.Product.PartNo,
            PartName = q.Product.PartName,
            Model = q.Model,
            NumberOff = q.NumberOff,
            Operations = operations,
            OtherCosts = otherCosts,
            ProcessCostTotal = q.ProcessCostTotal,
            EstimatedCostPerPart = q.EstimatedCostPerPart,
            GstRate = q.GstRate,
            DownloadUrl = q.StoredFilePath != null ? $"/api/files/quotation/{Uri.EscapeDataString(q.QuotationNo)}.pdf" : null,
            CreatedAt = q.CreatedAt
        };
    }
}
