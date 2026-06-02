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

    /// <summary>
    /// Compiles high-level financial KPIs, stock alerts, and top product performances isolated by store tenancy.
    /// </summary>
    public async Task<DashboardSummaryDto> GetLiveManagementDashboardAsync(string tenantId)
    {
        var todayUtc = DateTime.UtcNow.Date;

        // 1. Calculate high-level core KPI financial metrics for today (Enforced by TenantId)
        var todayOrdersQuery = _dbContext.Orders
            .Where(o => o.OrderDate >= todayUtc && o.TenantId == tenantId);

        var todayRevenue = await todayOrdersQuery.SumAsync(o => o.TotalAmount);
        var todayOrderCount = await todayOrdersQuery.CountAsync();
        var averageOrderValue = todayOrderCount > 0 ? todayRevenue / todayOrderCount : 0;

        // 2. Compute count of items running critically low on stock (Scoped by TenantId)
        // Note: Assumes your Product model has a TenantId or is linked to this tenant context
        var lowStockCount = await _dbContext.Products
            .Where(p => p.StockQuantity <= 5 && p.TenantId == tenantId)
            .CountAsync();

        // 3. Extract Top 5 selling items within this tenant's dataset
        var topProducts = await _dbContext.Orders
            .Where(o => o.TenantId == tenantId && o.Items != null) // ◄ Strict boundary before flattening items
            .SelectMany(o => o.Items)
            .Join(
                _dbContext.Products.Where(p => p.TenantId == tenantId), // ◄ Match only this tenant's inventory items
                item => item.ProductId,      // Foreign key on CloudOrderItem
                product => product.Id,       // Primary key on Product
                (item, product) => new { item, product }
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