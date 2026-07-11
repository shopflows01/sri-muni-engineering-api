using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Features.Products.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;
using ClosedXML.Excel;
using System.IO;

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

    public async Task<byte[]> GenerateExcelLedgerAsync(Guid productId, DateTime? fromDate, DateTime? toDate)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId)
            ?? throw new KeyNotFoundException($"Product with ID {productId} not found.");

        // Gather DCs
        var dcItemsQuery = _context.JobWorkDCItems
            .Include(i => i.JobWorkDC)
                .ThenInclude(d => d.Customer)
            .Include(i => i.Transactions)
            .Where(i => i.ProductId == productId);
            
        var dcItems = await dcItemsQuery.ToListAsync();
        
        var invoicesQuery = _context.InvoiceItems
            .Include(ii => ii.Invoice)
                .ThenInclude(i => i.Customer)
            .Where(ii => ii.ProductId == productId);
            
        var invoiceItems = await invoicesQuery.ToListAsync();

        var customerName = dcItems.FirstOrDefault()?.JobWorkDC?.Customer?.Name 
            ?? invoiceItems.FirstOrDefault()?.Invoice?.Customer?.Name 
            ?? "Unknown Customer";

        // Determine date range
        var today = DateTime.Today;
        var startDate = fromDate ?? new DateTime(today.Year, today.Month, 1);
        var endDate = toDate ?? new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        // Filter by date
        var filteredDcs = dcItems
            .Where(i => i.JobWorkDC.DcDate.Date >= startDate.Date && i.JobWorkDC.DcDate.Date <= endDate.Date)
            .Select(i => new { 
                Date = i.JobWorkDC.DcDate, 
                No = i.JobWorkDC.DcNo, 
                Qty = i.Transactions.Where(t => t.TransactionType == Domain.Enums.TransactionType.Inward).Sum(t => t.Quantity) 
            })
            .Where(x => x.Qty > 0)
            .OrderBy(x => x.Date)
            .ToList();

        var filteredInvoices = invoiceItems
            .Where(ii => ii.Invoice.Date.Date >= startDate.Date && ii.Invoice.Date.Date <= endDate.Date)
            .Select(ii => new { 
                Date = ii.Invoice.Date, 
                No = ii.Invoice.InvoiceNo, 
                Qty = ii.Quantity 
            })
            .OrderBy(x => x.Date)
            .ToList();

        // Totals
        var totalInward = filteredDcs.Sum(d => d.Qty);
        var totalOutward = filteredInvoices.Sum(i => i.Qty);
        var balance = totalInward - totalOutward;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Ledger");

        // Row 1-4 Merge A-F
        var titleRange = ws.Range("A1:F4");
        titleRange.Merge();
        
        var richText = ws.Cell("A1").GetRichText();
        richText.ClearText();
        richText.AddText("From: ").SetFontName("Calibri").SetFontSize(11).SetFontColor(XLColor.Black).SetBold(false);
        richText.AddText(customerName).SetFontName("Calibri").SetFontSize(14).SetFontColor(XLColor.Purple).SetBold(true);
        richText.AddText(Environment.NewLine + "To: ").SetFontName("Calibri").SetFontSize(11).SetFontColor(XLColor.Black).SetBold(false);
        richText.AddText("SRI VALLI INDUSTRIES").SetFontName("Calibri").SetFontSize(14).SetFontColor(XLColor.Purple).SetBold(true);
        
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Cell("A1").Style.Alignment.WrapText = true;

        // Insert Image
        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "frontend", "public", "assets", "excel-logo.png");
        if (System.IO.File.Exists(logoPath))
        {
            var picture = ws.AddPicture(logoPath);
            picture.MoveTo(ws.Cell("A1"));
            picture.Scale(0.8); // Adjust scale
        }

        // Row 5 Merge A-F
        var productRange = ws.Range("A5:F5");
        productRange.Merge();
        productRange.Value = $"{product.PartName} - {product.PartNo}";
        productRange.Style.Font.Bold = true;
        productRange.Style.Font.FontSize = 12;
        productRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Row 6 Headers
        ws.Cell("A6").Value = "DC Date";
        ws.Cell("B6").Value = "DC No";
        ws.Cell("C6").Value = "Qty";
        ws.Cell("D6").Value = "Invoice Date";
        ws.Cell("E6").Value = "Invoice No";
        ws.Cell("F6").Value = "Qty";
        
        var headerRange = ws.Range("A6:F6");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int currentRow = 7;

        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var dcsForDate = filteredDcs.Where(d => d.Date.Date == date).ToList();
            var invoicesForDate = filteredInvoices.Where(i => i.Date.Date == date).ToList();
            
            int maxForDate = Math.Max(dcsForDate.Count, invoicesForDate.Count);
            
            if (maxForDate == 0)
            {
                ws.Cell(currentRow, 1).Value = date.ToString("dd-MMM-yyyy");
                ws.Cell(currentRow, 2).Value = "-";
                ws.Cell(currentRow, 3).Value = 0;
                
                ws.Cell(currentRow, 4).Value = date.ToString("dd-MMM-yyyy");
                ws.Cell(currentRow, 5).Value = "-";
                ws.Cell(currentRow, 6).Value = 0;
                
                currentRow++;
            }
            else
            {
                for (int i = 0; i < maxForDate; i++)
                {
                    if (i < dcsForDate.Count)
                    {
                        ws.Cell(currentRow, 1).Value = dcsForDate[i].Date.ToString("dd-MMM-yyyy");
                        ws.Cell(currentRow, 2).Value = dcsForDate[i].No;
                        ws.Cell(currentRow, 3).Value = dcsForDate[i].Qty;
                    }
                    else
                    {
                        ws.Cell(currentRow, 1).Value = date.ToString("dd-MMM-yyyy");
                        ws.Cell(currentRow, 2).Value = "-";
                        ws.Cell(currentRow, 3).Value = 0;
                    }

                    if (i < invoicesForDate.Count)
                    {
                        ws.Cell(currentRow, 4).Value = invoicesForDate[i].Date.ToString("dd-MMM-yyyy");
                        ws.Cell(currentRow, 5).Value = invoicesForDate[i].No;
                        ws.Cell(currentRow, 6).Value = invoicesForDate[i].Qty;
                    }
                    else
                    {
                        ws.Cell(currentRow, 4).Value = date.ToString("dd-MMM-yyyy");
                        ws.Cell(currentRow, 5).Value = "-";
                        ws.Cell(currentRow, 6).Value = 0;
                    }
                    currentRow++;
                }
            }
        }

        // Totals
        ws.Cell(currentRow, 2).Value = "Total:";
        ws.Cell(currentRow, 2).Style.Font.Bold = true;
        ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(currentRow, 3).Value = totalInward;
        ws.Cell(currentRow, 3).Style.Font.Bold = true;

        ws.Cell(currentRow, 5).Value = "Total:";
        ws.Cell(currentRow, 5).Style.Font.Bold = true;
        ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(currentRow, 6).Value = totalOutward;
        ws.Cell(currentRow, 6).Style.Font.Bold = true;
        
        currentRow++;

        // Balance Stock
        var balanceRange = ws.Range($"A{currentRow}:E{currentRow}");
        balanceRange.Merge();
        balanceRange.Value = "Balance Stock:";
        balanceRange.Style.Font.Bold = true;
        balanceRange.Style.Font.FontColor = XLColor.Red;
        balanceRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        ws.Cell(currentRow, 6).Value = balance;
        ws.Cell(currentRow, 6).Style.Font.Bold = true;
        ws.Cell(currentRow, 6).Style.Font.FontColor = XLColor.Red;

        // Apply Borders
        var dataRange = ws.Range($"A1:F{currentRow}");
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        ws.Columns(1, 6).AdjustToContents();

        // Print settings
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
