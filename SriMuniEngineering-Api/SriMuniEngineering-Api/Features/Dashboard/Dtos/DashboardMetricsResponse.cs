namespace SriMuniEngineering_Api.Features.Dashboard.Dtos;

public class DashboardMetricsResponse
{
    // ─── Big Numbers (Today) ──────────────────────────────────
    public int TodayInwardQty { get; set; }
    public int TodayOutwardQty { get; set; }
    public int TodayRejectedQty { get; set; }

    // ─── Big Numbers (Overall Monthly) ────────────────────────
    public int MonthlyInwardQty { get; set; }
    public int MonthlyOutwardQty { get; set; }
    public int MonthlyRejectedQty { get; set; }

    // ─── Pending Counts ───────────────────────────────────────
    public int PendingInvoicesCount { get; set; }
    public int InProgressLedgerCount { get; set; }

    // ─── Totals ───────────────────────────────────────────────
    public int TotalCustomers { get; set; }
    public int TotalProducts { get; set; }
    public decimal MonthlyRevenueTotal { get; set; }

    // ─── Chart Data ───────────────────────────────────────────
    public List<DailyQuantityPoint> DailyInwardChart { get; set; } = [];
    public List<DailyQuantityPoint> DailyOutwardChart { get; set; } = [];
    public List<DailyQuantityPoint> DailyRejectionChart { get; set; } = [];
    public List<MonthlyRevenuePoint> MonthlyRevenueChart { get; set; } = [];
    public List<StatusBreakdown> LedgerStatusBreakdown { get; set; } = [];
    public List<TopCustomerMetric> TopCustomersByVolume { get; set; } = [];
}

public class DailyQuantityPoint
{
    public DateOnly Date { get; set; }
    public int Quantity { get; set; }
}

public class MonthlyRevenuePoint
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public class StatusBreakdown
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopCustomerMetric
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalInwardQty { get; set; }
    public int TotalInvoices { get; set; }
}
