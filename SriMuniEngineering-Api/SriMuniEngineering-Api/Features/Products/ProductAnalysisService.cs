using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Features.Products.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Products;

public class ProductAnalysisService
{
    private readonly AppDbContext _context;

    public ProductAnalysisService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ProductAnalysisResponse> GetAnalysisAsync(Guid productId)
    {
        // ─── Validate product exists ─────────────────────────
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // ─── Production data (JobWorkLedger) ─────────────────
        var ledgers = await _context.JobWorkLedgers
            .Include(l => l.Customer)
            .Where(l => l.ProductId == productId)
            .OrderByDescending(l => l.DcDate)
            .ToListAsync();

        var totalInward = ledgers.Sum(l => l.InwardQty);
        var totalOutward = ledgers.Sum(l => l.OutwardQty);
        var totalRejected = ledgers.Sum(l => l.RejectedQty);
        var productionDays = ledgers.Select(l => l.DcDate.Date).Distinct().Count();
        var lastProductionDate = ledgers.FirstOrDefault()?.DcDate;

        // ─── Sales data (InvoiceItems) ───────────────────────
        var invoiceItems = await _context.InvoiceItems
            .Include(ii => ii.Invoice)
                .ThenInclude(i => i.Customer)
            .Where(ii => ii.ProductId == productId)
            .OrderByDescending(ii => ii.Invoice.Date)
            .ToListAsync();

        var totalQtySold = invoiceItems.Sum(ii => ii.Quantity);
        var totalRevenue = invoiceItems.Sum(ii => ii.Amount);
        var invoiceCount = invoiceItems.Select(ii => ii.InvoiceId).Distinct().Count();
        var avgSellingPrice = totalQtySold > 0 ? totalRevenue / totalQtySold : 0;
        var lastSoldDate = invoiceItems.FirstOrDefault()?.Invoice.Date;

        // ─── Build Response ──────────────────────────────────
        var currentStock = totalInward - totalOutward - totalRejected;

        return new ProductAnalysisResponse
        {
            ProductInfo = new ProductInfoDto
            {
                ProductId = product.Id,
                PartNo = product.PartNo,
                PartName = product.PartName,
                PartDescription = product.PartDescription,
                CurrentStock = currentStock,
                Unit = product.Unit,
                SellingPrice = product.BasePricePerUnit,
                HsnSac = product.HsnSac
            },

            ProductionSummary = new ProductionSummaryDto
            {
                TotalProductionQuantity = totalOutward,
                TotalRejectedQuantity = totalRejected,
                ProductionDaysCount = productionDays,
                AverageProductionPerDay = productionDays > 0
                    ? Math.Round((decimal)totalOutward / productionDays, 2)
                    : 0,
                LastProductionDate = lastProductionDate
            },

            SalesSummary = new SalesSummaryDto
            {
                TotalQuantitySold = totalQtySold,
                TotalRevenueGenerated = totalRevenue,
                NumberOfInvoices = invoiceCount,
                AverageSellingPrice = Math.Round(avgSellingPrice, 4),
                LastSoldDate = lastSoldDate
            },

            StockSummary = new StockSummaryDto
            {
                CurrentStock = currentStock,
                TotalInward = totalInward,
                TotalOutward = totalOutward
            },

            // Recent production history (last 20)
            RecentProductionHistory = ledgers
                .Take(20)
                .Select(l => new ProductionHistoryItem
                {
                    LedgerId = l.Id,
                    DcNo = l.DcNo,
                    DcDate = l.DcDate,
                    CustomerName = l.Customer.Name,
                    InwardQty = l.InwardQty,
                    OutwardQty = l.OutwardQty,
                    RejectedQty = l.RejectedQty,
                    Status = l.Status.ToString()
                }).ToList(),

            // Recent invoice history for this product (last 20)
            RecentInvoiceHistory = invoiceItems
                .Take(20)
                .Select(ii => new InvoiceHistoryItem
                {
                    InvoiceId = ii.InvoiceId,
                    InvoiceNo = ii.Invoice.InvoiceNo,
                    InvoiceDate = ii.Invoice.Date,
                    CustomerName = ii.Invoice.Customer.Name,
                    Quantity = ii.Quantity,
                    Rate = ii.Rate,
                    Amount = ii.Amount
                }).ToList(),

            // Recent stock movements (last 20 ledger entries as movements)
            RecentStockMovements = BuildStockMovements(ledgers.Take(20).ToList())
        };
    }

    private static List<StockMovementItem> BuildStockMovements(List<Domain.Entities.JobWorkLedger> ledgers)
    {
        var movements = new List<StockMovementItem>();

        foreach (var l in ledgers)
        {
            if (l.InwardQty > 0)
            {
                movements.Add(new StockMovementItem
                {
                    LedgerId = l.Id,
                    DcNo = l.DcNo,
                    DcDate = l.DcDate,
                    CustomerName = l.Customer.Name,
                    MovementType = "Inward",
                    Quantity = l.InwardQty
                });
            }

            if (l.OutwardQty > 0)
            {
                movements.Add(new StockMovementItem
                {
                    LedgerId = l.Id,
                    DcNo = l.DcNo,
                    DcDate = l.DcDate,
                    CustomerName = l.Customer.Name,
                    MovementType = "Outward",
                    Quantity = l.OutwardQty
                });
            }

            if (l.RejectedQty > 0)
            {
                movements.Add(new StockMovementItem
                {
                    LedgerId = l.Id,
                    DcNo = l.DcNo,
                    DcDate = l.DcDate,
                    CustomerName = l.Customer.Name,
                    MovementType = "Rejected",
                    Quantity = l.RejectedQty
                });
            }
        }

        return movements.OrderByDescending(m => m.DcDate).ToList();
    }
}
