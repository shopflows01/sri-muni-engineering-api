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

        // ─── Production data (JobWorkDCs) ─────────────────
        var items = await _context.JobWorkDCItems
            .Include(i => i.JobWorkDC)
                .ThenInclude(d => d.Customer)
            .Include(i => i.Transactions)
            .Where(i => i.ProductId == productId)
            .ToListAsync();

        var allTransactions = items.SelectMany(i => i.Transactions).ToList();
        var totalInward = allTransactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Inward).Sum(t => t.Quantity);
        var totalOutward = allTransactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Outward).Sum(t => t.Quantity);
        var totalRejected = allTransactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Rejected).Sum(t => t.Quantity);
        
        var dcs = items.Select(i => i.JobWorkDC).DistinctBy(d => d.Id).ToList();
        var productionDays = dcs.Select(d => d.DcDate.Date).Distinct().Count();
        var lastProductionDate = dcs.OrderByDescending(d => d.DcDate).FirstOrDefault()?.DcDate;

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
                HsnSac = product.HsnSac,
                Customers = dcs
                    .Select(d => new ProductCustomerDto { CustomerId = d.Customer.Id, CustomerName = d.Customer.Name })
                    .DistinctBy(c => c.CustomerId)
                    .ToList()
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
            RecentProductionHistory = items
                .OrderByDescending(i => i.JobWorkDC.DcDate)
                .Take(20)
                .Select(i => new ProductionHistoryItem
                {
                    LedgerId = i.Id,
                    DcNo = i.JobWorkDC.DcNo,
                    DcDate = i.JobWorkDC.DcDate,
                    CustomerName = i.JobWorkDC.Customer.Name,
                    InwardQty = i.Transactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Inward).Sum(t => t.Quantity),
                    OutwardQty = i.Transactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Outward).Sum(t => t.Quantity),
                    RejectedQty = i.Transactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Rejected).Sum(t => t.Quantity),
                    Status = i.JobWorkDC.Status.ToString()
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
            RecentStockMovements = BuildStockMovements(items.Take(20).ToList())
        };
    }

    private static List<StockMovementItem> BuildStockMovements(List<Domain.Entities.JobWorkDCItem> items)
    {
        var movements = new List<StockMovementItem>();

        foreach (var i in items)
        {
            foreach (var t in i.Transactions)
            {
                movements.Add(new StockMovementItem
                {
                    LedgerId = i.Id,
                    DcNo = i.JobWorkDC.DcNo,
                    DcDate = i.JobWorkDC.DcDate,
                    CustomerName = i.JobWorkDC.Customer.Name,
                    MovementType = t.TransactionType.ToString(),
                    Quantity = t.Quantity
                });
            }
        }

        return movements.OrderByDescending(m => m.DcDate).ToList();
    }
}
