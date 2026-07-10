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
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Local);
        var last30Days = today.AddDays(-30);

        // ─── Big Numbers: Today ───────────────────────────────
        var todayTransactions = await _context.JobWorkTransactions
            .Where(t => t.TransactionDate >= today && t.TransactionDate < today.AddDays(1))
            .ToListAsync();

        var todayInward = todayTransactions.Where(t => t.TransactionType == TransactionType.Inward).Sum(t => t.Quantity);
        var todayOutward = todayTransactions.Where(t => t.TransactionType == TransactionType.Outward).Sum(t => t.Quantity);
        var todayRejected = todayTransactions.Where(t => t.TransactionType == TransactionType.Rejected).Sum(t => t.Quantity);

        // ─── Big Numbers: Monthly ─────────────────────────────
        var monthTransactions = await _context.JobWorkTransactions
            .Where(t => t.TransactionDate >= monthStart)
            .ToListAsync();

        var monthlyInward = monthTransactions.Where(t => t.TransactionType == TransactionType.Inward).Sum(t => t.Quantity);
        var monthlyOutward = monthTransactions.Where(t => t.TransactionType == TransactionType.Outward).Sum(t => t.Quantity);
        var monthlyRejected = monthTransactions.Where(t => t.TransactionType == TransactionType.Rejected).Sum(t => t.Quantity);

        // ─── Pending Counts ───────────────────────────────────
        var pendingInvoices = await _context.Invoices.CountAsync(i => i.StoredFilePath == null);
        var inProgressLedgers = await _context.JobWorkDCs.CountAsync(d => d.Status == LedgerStatus.InProgress);

        // ─── Totals ───────────────────────────────────────────
        var totalCustomers = await _context.Customers.CountAsync();
        var totalProducts = await _context.Products.CountAsync();
        var monthlyRevenue = await _context.Invoices
            .Where(i => i.Date >= monthStart)
            .SumAsync(i => i.GrandTotal);

        // ─── Chart: Daily Inward/Outward/Rejection (last 30 days) ─
        var dailyData = await _context.JobWorkTransactions
            .Where(t => t.TransactionDate >= last30Days)
            .GroupBy(t => t.TransactionDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Inward = g.Where(x => x.TransactionType == TransactionType.Inward).Sum(x => x.Quantity),
                Outward = g.Where(x => x.TransactionType == TransactionType.Outward).Sum(x => x.Quantity),
                Rejected = g.Where(x => x.TransactionType == TransactionType.Rejected).Sum(x => x.Quantity)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        var dailyInwardChart = dailyData
            .Where(d => d.Inward > 0)
            .Select(d => new DailyQuantityPoint
            {
                Date = DateOnly.FromDateTime(d.Date),
                Quantity = d.Inward
            }).ToList();

        var dailyOutwardChart = dailyData
            .Where(d => d.Outward > 0)
            .Select(d => new DailyQuantityPoint
            {
                Date = DateOnly.FromDateTime(d.Date),
                Quantity = d.Outward
            }).ToList();

        var dailyRejectionChart = dailyData
            .Where(d => d.Rejected > 0)
            .Select(d => new DailyQuantityPoint
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
                Revenue = g.Sum(x => x.GrandTotal)
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
        var statusBreakdown = await _context.JobWorkDCs
            .GroupBy(d => d.Status)
            .Select(g => new StatusBreakdown
            {
                Status = g.Key.ToString(),
                Count = g.Count()
            })
            .ToListAsync();

        // ─── Chart: Top 5 Customers by Volume ────────────────
        var topCustomers = await _context.JobWorkDCItems
            .Include(i => i.JobWorkDC)
                .ThenInclude(d => d.Customer)
            .Include(i => i.Transactions)
            .SelectMany(i => i.Transactions, (item, trans) => new { item, trans })
            .Where(x => x.trans.TransactionType == TransactionType.Inward)
            .GroupBy(x => new { x.item.JobWorkDC.CustomerId, x.item.JobWorkDC.Customer.Name })
            .Select(g => new TopCustomerMetric
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.Name,
                TotalInwardQty = g.Sum(x => x.trans.Quantity),
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
