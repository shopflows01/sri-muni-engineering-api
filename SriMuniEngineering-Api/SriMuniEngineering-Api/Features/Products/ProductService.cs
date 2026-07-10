using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Common;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.Products.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Products;

public class ProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        var existing = await _context.Products.FirstOrDefaultAsync(p => p.PartNo == request.PartNo);
        if (existing is not null)
            throw new InvalidOperationException($"Product with PartNo '{request.PartNo}' already exists.");

        var product = new Product
        {
            Id = Guid.NewGuid(),
            PartNo = request.PartNo,
            PartName = request.PartName,
            PartDescription = request.PartDescription,
            BasePricePerUnit = request.BasePricePerUnit,
            RatePerItem = request.RatePerItem,
            GstPercent = request.GstPercent,
            HsnSac = request.HsnSac,
            Unit = request.Unit
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        product.PartNo = request.PartNo;
        product.PartName = request.PartName;
        product.PartDescription = request.PartDescription;
        product.BasePricePerUnit = request.BasePricePerUnit;
        product.RatePerItem = request.RatePerItem;
        product.GstPercent = request.GstPercent;
        product.HsnSac = request.HsnSac;
        product.Unit = request.Unit;

        await _context.SaveChangesAsync();
        return MapToResponse(product);
    }

    public async Task<ProductResponse> GetByIdAsync(Guid id)
    {
        var product = await _context.Products
            .Include(p => p.JobWorkDCItems)
                .ThenInclude(i => i.JobWorkDC)
                    .ThenInclude(d => d.Customer)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        return MapToResponse(product);
    }

    public async Task<PaginatedResponse<ProductResponse>> GetAllAsync(PaginatedRequest filter)
    {
        var query = _context.Products
            .Include(p => p.JobWorkDCItems)
                .ThenInclude(i => i.JobWorkDC)
                    .ThenInclude(d => d.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p =>
                p.PartNo.Contains(filter.Search) ||
                p.PartName.Contains(filter.Search) ||
                p.HsnSac.Contains(filter.Search));

        var isAsc = filter.SortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
        query = filter.SortBy?.ToLower() switch
        {
            "partno" => isAsc ? query.OrderBy(p => p.PartNo) : query.OrderByDescending(p => p.PartNo),
            "partname" => isAsc ? query.OrderBy(p => p.PartName) : query.OrderByDescending(p => p.PartName),
            "price" => isAsc ? query.OrderBy(p => p.BasePricePerUnit) : query.OrderByDescending(p => p.BasePricePerUnit),
            _ => query.OrderBy(p => p.PartNo)
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PaginatedResponse<ProductResponse>
        {
            Items = items.Select(MapToResponse).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException($"Product with ID {id} not found.");

        product.IsDeleted = true;
        product.DeletedAt = DateTime.Now;
        await _context.SaveChangesAsync();
    }

    private static ProductResponse MapToResponse(Product p) => new()
    {
        Id = p.Id,
        PartNo = p.PartNo,
        PartName = p.PartName,
        PartDescription = p.PartDescription,
        BasePricePerUnit = p.BasePricePerUnit,
        RatePerItem = p.RatePerItem,
        GstPercent = p.GstPercent,
        HsnSac = p.HsnSac,
        Unit = p.Unit,
        IsDeleted = p.IsDeleted,
        DeletedAt = p.DeletedAt,
        Customers = (p.JobWorkDCItems ?? [])
            .Select(i => new ProductCustomerDto { CustomerId = i.JobWorkDC.CustomerId, CustomerName = i.JobWorkDC.Customer.Name })
            .DistinctBy(c => c.CustomerId)
            .ToList()
    };
}
