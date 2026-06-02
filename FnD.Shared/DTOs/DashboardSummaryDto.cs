using System;
using System.Collections.Generic;
using System.Text;

namespace FnD.Shared.DTOs;

public class DashboardSummaryDto
{
    public decimal TodayRevenue { get; set; }
    public int TodayOrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public List<TopProductDto> TopSellingProducts { get; set; } = new();
    public int LowStockAlertCount { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenueGenerated { get; set; }
}