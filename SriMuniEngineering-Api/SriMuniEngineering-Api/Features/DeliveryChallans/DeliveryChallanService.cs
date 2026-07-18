using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Entities;
using SriMuniEngineering_Api.Features.DeliveryChallans.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.DeliveryChallans;

public class DeliveryChallanService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public DeliveryChallanService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<List<DeliveryChallanResponse>> GetAllAsync()
    {
        var dcs = await _context.DeliveryChallans
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(d => d.DcDate)
            .ThenByDescending(d => d.CreatedDate)
            .ToListAsync();

        return dcs.Select(MapToResponse).ToList();
    }

    public async Task<DeliveryChallanResponse?> GetByIdAsync(Guid id)
    {
        var dc = await _context.DeliveryChallans
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        return dc != null ? MapToResponse(dc) : null;
    }

    public async Task<DeliveryChallanResponse> CreateAsync(CreateDeliveryChallanRequest request, string username)
    {
        var dc = new DeliveryChallan
        {
            Id = Guid.NewGuid(),
            DcNo = await GenerateDcNoAsync(),
            CustomerId = request.CustomerId,
            DcDate = request.DcDate,
            YourDcNo = request.YourDcNo,
            YourDcDate = request.YourDcDate,
            PoNo = request.PoNo,
            Remarks = request.Remarks,
            CreatedBy = username,
            CreatedDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            dc.Items.Add(new DeliveryChallanItem
            {
                Id = Guid.NewGuid(),
                DeliveryChallanId = dc.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Remarks = item.Remarks
            });
        }

        _context.DeliveryChallans.Add(dc);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(dc.Id) ?? throw new InvalidOperationException("Failed to retrieve created DC.");
    }

    public async Task<DeliveryChallanResponse?> UpdateAsync(Guid id, CreateDeliveryChallanRequest request)
    {
        var dc = await _context.DeliveryChallans
            .Include(d => d.Items)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dc == null) return null;

        dc.CustomerId = request.CustomerId;
        dc.DcDate = request.DcDate;
        dc.YourDcNo = request.YourDcNo;
        dc.YourDcDate = request.YourDcDate;
        dc.PoNo = request.PoNo;
        dc.Remarks = request.Remarks;

        // Remove existing items
        _context.DeliveryChallanItems.RemoveRange(dc.Items);

        // Add new items
        foreach (var item in request.Items)
        {
            dc.Items.Add(new DeliveryChallanItem
            {
                Id = Guid.NewGuid(),
                DeliveryChallanId = dc.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Remarks = item.Remarks
            });
        }

        await _context.SaveChangesAsync();

        return await GetByIdAsync(dc.Id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var dc = await _context.DeliveryChallans.FindAsync(id);
        if (dc == null) return false;

        _context.DeliveryChallans.Remove(dc);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid id)
    {
        var dc = await _context.DeliveryChallans
            .Include(d => d.Customer)
            .Include(d => d.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dc == null)
            throw new KeyNotFoundException("Delivery Challan not found");

        var companyProfile = _configuration.GetSection("CompanyProfile");
        return DeliveryChallanPdfGenerator.Generate(dc, companyProfile);
    }

    private async Task<string> GenerateDcNoAsync()
    {
        var lastDc = await _context.DeliveryChallans
            .OrderByDescending(d => d.DcNo)
            .FirstOrDefaultAsync();

        if (lastDc == null || !int.TryParse(lastDc.DcNo, out int currentNumber))
        {
            return "001";
        }

        return (currentNumber + 1).ToString("D3");
    }

    private static DeliveryChallanResponse MapToResponse(DeliveryChallan dc)
    {
        return new DeliveryChallanResponse(
            Id: dc.Id,
            DcNo: dc.DcNo,
            CustomerId: dc.CustomerId,
            CustomerName: dc.Customer?.Name ?? "",
            DcDate: dc.DcDate,
            YourDcNo: dc.YourDcNo,
            YourDcDate: dc.YourDcDate,
            PoNo: dc.PoNo,
            Remarks: dc.Remarks,
            Items: dc.Items.Select(i => new DeliveryChallanItemResponse(
                Id: i.Id,
                ProductId: i.ProductId,
                PartNo: i.Product?.PartNo ?? "",
                PartName: i.Product?.PartName ?? "",
                Quantity: i.Quantity,
                Unit: i.Unit,
                Remarks: i.Remarks
            )).ToList()
        );
    }
}
