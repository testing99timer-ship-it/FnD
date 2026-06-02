using FnD.Cloud.API.Data;
using FnD.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace FnD.Cloud.API.Services;

public class DashboardReportingService
{
    private readonly CloudDbContext _dbContext;

    public DashboardReportingService(CloudDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardSummaryDto> GetLiveManagementDashboardAsync()
    {
        var todayUtc = DateTime.UtcNow.Date;

        // 1. Calculate high-level core KPI financial metrics for today
        var todayOrdersQuery = _dbContext.Orders
            .Where(o => o.OrderDate >= todayUtc);

        var todayRevenue = await todayOrdersQuery.SumAsync(o => o.TotalAmount);
        var todayOrderCount = await todayOrdersQuery.CountAsync();
        var averageOrderValue = todayOrderCount > 0 ? todayRevenue / todayOrderCount : 0;

        // 2. Compute count of items that are running critically low on stock (e.g., < 5 items)
        var lowStockCount = await _dbContext.Products
            .Where(p => p.StockQuantity <= 5)
            .CountAsync();

        // 3. Extract Top 5 selling items using an explicit LINQ Join to bypass missing navigation properties
        var topProducts = await _dbContext.Orders
            .Where(o => o.Items != null)
            .SelectMany(o => o.Items)
            .Join(
                _dbContext.Products,
                item => item.ProductId,      // Foreign key on CloudOrderItem
                product => product.Id,       // Primary key on Product
                (item, product) => new { item, product } // Combine them into a flat anonymous type
            )
            .GroupBy(joined => new { joined.item.ProductId, joined.product.Name })
            .Select(group => new TopProductDto
            {
                ProductId = group.Key.ProductId,
                ProductName = group.Key.Name,
                TotalQuantitySold = group.Sum(x => x.item.Quantity),
                TotalRevenueGenerated = group.Sum(x => x.item.Quantity * x.item.UnitPrice)
            })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(5)
            .ToListAsync();

        return new DashboardSummaryDto
        {
            TodayRevenue = todayRevenue,
            TodayOrderCount = todayOrderCount,
            AverageOrderValue = Math.Round(averageOrderValue, 2),
            LowStockAlertCount = lowStockCount,
            TopSellingProducts = topProducts
        };
    }
}