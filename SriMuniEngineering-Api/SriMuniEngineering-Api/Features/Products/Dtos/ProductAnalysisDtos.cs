namespace SriMuniEngineering_Api.Features.Products.Dtos;

public class ProductAnalysisResponse
{
    // ─── Product Information ──────────────────────────────────
    public ProductInfoDto ProductInfo { get; set; } = new();

    // ─── Production Summary ───────────────────────────────────
    public ProductionSummaryDto ProductionSummary { get; set; } = new();

    // ─── Sales Summary ────────────────────────────────────────
    public SalesSummaryDto SalesSummary { get; set; } = new();

    // ─── Stock Summary ────────────────────────────────────────
    public StockSummaryDto StockSummary { get; set; } = new();

    // ─── History ──────────────────────────────────────────────
    public List<ProductionHistoryItem> RecentProductionHistory { get; set; } = [];
    public List<InvoiceHistoryItem> RecentInvoiceHistory { get; set; } = [];
    public List<StockMovementItem> RecentStockMovements { get; set; } = [];
}

public class ProductInfoDto
{
    public Guid ProductId { get; set; }
    public string PartNo { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? PartDescription { get; set; }
    public int CurrentStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public string HsnSac { get; set; } = string.Empty;
    public List<ProductCustomerDto> Customers { get; set; } = [];
}

public class ProductionSummaryDto
{
    public int TotalProductionQuantity { get; set; }
    public int TotalRejectedQuantity { get; set; }
    public int ProductionDaysCount { get; set; }
    public decimal AverageProductionPerDay { get; set; }
    public DateTime? LastProductionDate { get; set; }
}

public class SalesSummaryDto
{
    public decimal TotalQuantitySold { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
    public int NumberOfInvoices { get; set; }
    public decimal AverageSellingPrice { get; set; }
    public DateTime? LastSoldDate { get; set; }
}

public class StockSummaryDto
{
    public int CurrentStock { get; set; }
    public int TotalInward { get; set; }
    public int TotalOutward { get; set; }
}

public class ProductionHistoryItem
{
    public Guid LedgerId { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int InwardQty { get; set; }
    public int OutwardQty { get; set; }
    public int RejectedQty { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class InvoiceHistoryItem
{
    public Guid InvoiceId { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
}

public class StockMovementItem
{
    public Guid LedgerId { get; set; }
    public string DcNo { get; set; } = string.Empty;
    public DateTime DcDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string MovementType { get; set; } = string.Empty;
    public int Quantity { get; set; }
}
