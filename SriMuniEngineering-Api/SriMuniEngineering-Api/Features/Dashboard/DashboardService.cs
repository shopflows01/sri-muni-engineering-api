using Microsoft.EntityFrameworkCore;
using SriMuniEngineering_Api.Domain.Enums;
using SriMuniEngineering_Api.Features.Dashboard.Dtos;
using SriMuniEngineering_Api.Infrastructure.Data;

namespace SriMuniEngineering_Api.Features.Dashboard;

public class DashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardMetricsResponse> GetMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var last30Days = today.AddDays(-30);

        // ─── Big Numbers: Today ───────────────────────────────
        var todayLedgers = await _context.JobWorkLedgers
            .Where(l => l.DcDate >= today && l.DcDate < today.AddDays(1))
            .ToListAsync();

        var todayInward = todayLedgers.Sum(l => l.InwardQty);
        var todayOutward = todayLedgers.Sum(l => l.OutwardQty);
        var todayRejected = todayLedgers.Sum(l => l.RejectedQty);

        // ─── Big Numbers: Monthly ─────────────────────────────
        var monthLedgers = await _context.JobWorkLedgers
            .Where(l => l.DcDate >= monthStart)
            .ToListAsync();

        var monthlyInward = monthLedgers.Sum(l => l.InwardQty);
        var monthlyOutward = monthLedgers.Sum(l => l.OutwardQty);
        var monthlyRejected = monthLedgers.Sum(l => l.RejectedQty);

        // ─── Pending Counts ───────────────────────────────────
        var pendingInvoices = await _context.Invoices.CountAsync(i => i.StoredFilePath == null);
        var inProgressLedgers = await _context.JobWorkLedgers.CountAsync(l => l.Status == LedgerStatus.InProgress);

        // ─── Totals ───────────────────────────────────────────
        var totalCustomers = await _context.Customers.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        var monthlyRevenue = await _context.Invoices
            .Where(i => i.Date >= monthStart)
            .SumAsync(i => i.TotalAmount);

        // ─── Chart: Daily Inward/Outward/Rejection (last 30 days) ─
        var dailyData = await _context.JobWorkLedgers
            .Where(l => l.DcDate >= last30Days)
            .GroupBy(l => l.DcDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Inward = g.Sum(x => x.InwardQty),
                Outward = g.Sum(x => x.OutwardQty),
                Rejected = g.Sum(x => x.RejectedQty)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dailyInwardChart = dailyData.Select(d => new DailyQuantityPoint
        {
            Date = DateOnly.FromDateTime(d.Date),
            Quantity = d.Inward
        }).ToList();

        var dailyOutwardChart = dailyData.Select(d => new DailyQuantityPoint
        {
            Date = DateOnly.FromDateTime(d.Date),
            Quantity = d.Outward
        }).ToList();

        var dailyRejectionChart = dailyData.Select(d => new DailyQuantityPoint
        {
            Date = DateOnly.FromDateTime(d.Date),
            Quantity = d.Rejected
        }).ToList();

        // ─── Chart: Monthly Revenue (last 6 months) ──────────
        var sixMonthsAgo = today.AddMonths(-6);
        var monthlyRevenueChart = (await _context.Invoices
            .Where(i => i.Date >= sixMonthsAgo)
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync())
            .Select(x => new MonthlyRevenuePoint
            {
                Month = $"{x.Year}-{x.Month:D2}",
                Revenue = x.Revenue
            })
            .ToList();

        // ─── Chart: Ledger Status Breakdown (pie/donut) ──────
        var statusBreakdown = await _context.JobWorkLedgers
            .GroupBy(l => l.Status)
            .Select(g => new StatusBreakdown
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        // ─── Chart: Top 5 Customers by Volume ────────────────
        var topCustomers = await _context.JobWorkLedgers
            .Include(l => l.Customer)
            .GroupBy(l => new { l.CustomerId, l.Customer.Name })
            .Select(g => new TopCustomerMetric
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.Name,
                TotalInwardQty = g.Sum(x => x.InwardQty),
                TotalInvoices = 0
            })
            .OrderByDescending(x => x.TotalInwardQty)
            .Take(5)
            .ToListAsync();

        // Enrich with invoice counts
        var customerIds = topCustomers.Select(c => c.CustomerId).ToList();
        var invoiceCounts = await _context.Invoices
            .Where(i => customerIds.Contains(i.CustomerId))
            .GroupBy(i => i.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count() })
            .ToListAsync();

        foreach (var customer in topCustomers)
        {
            customer.TotalInvoices = invoiceCounts
                .FirstOrDefault(ic => ic.CustomerId == customer.CustomerId)?.Count ?? 0;
        }

        return new DashboardMetricsResponse
        {
            TodayInwardQty = todayInward,
            TodayOutwardQty = todayOutward,
            TodayRejectedQty = todayRejected,
            MonthlyInwardQty = monthlyInward,
            MonthlyOutwardQty = monthlyOutward,
            MonthlyRejectedQty = monthlyRejected,
            PendingInvoicesCount = pendingInvoices,
            InProgressLedgerCount = inProgressLedgers,
            TotalCustomers = totalCustomers,
            TotalProducts = totalProducts,
            MonthlyRevenueTotal = monthlyRevenue,
            DailyInwardChart = dailyInwardChart,
            DailyOutwardChart = dailyOutwardChart,
            DailyRejectionChart = dailyRejectionChart,
            MonthlyRevenueChart = monthlyRevenueChart,
            LedgerStatusBreakdown = statusBreakdown,
            TopCustomersByVolume = topCustomers
        };
    }
}
