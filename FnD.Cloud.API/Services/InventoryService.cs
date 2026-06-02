using FnD.Cloud.API.Data;
using FnD.Cloud.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FnD.Cloud.API.Services;

public class InventoryService
{
    private readonly CloudDbContext _dbContext;
    private readonly ILogger<InventoryService> _logger;
    private readonly NotificationService _notifier; // <-- Add this dependency

    public InventoryService(CloudDbContext dbContext, ILogger<InventoryService> logger, NotificationService notifier)
    {
        _dbContext = dbContext;
        _logger = logger;
        _notifier = notifier;
    }

    /// <summary>
    /// Aggregates all items across a batch of synced orders and deducts stock levels safely.
    /// </summary>
    public async Task ProcessInventoryDeductionsAsync(List<CloudOrder> orders)
    {
        // 1. Flatten all nested items across all incoming orders and group by Product ID
        var stockDeductions = orders
            .Where(o => o.Items != null)
            .SelectMany(o => o.Items)
            .GroupBy(item => item.ProductId)
            .Select(group => new
            {
                ProductId = group.Key,
                TotalDeduction = group.Sum(item => item.Quantity)
            })
            .ToList();

        if (!stockDeductions.Any()) return;

        // 2. Fetch only the impacted products from SSMS in a single database roundtrip
        var productIds = stockDeductions.Select(d => d.ProductId).ToList();
        var products = await _dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        // 3. Process adjustments and check for anomalies (like running out of stock)
        foreach (var deduction in stockDeductions)
        {
            var product = products.FirstOrDefault(p => p.Id == deduction.ProductId);
            if (product != null)
            {
                product.StockQuantity -= deduction.TotalDeduction;

                // Inside your ProcessInventoryDeductionsAsync method, update the negative stock check:
                if (product.StockQuantity < 0)
                {
                    _logger.LogWarning("Inventory Alert: Product ID {Id} ('{Name}') has fallen into negative stock ({Quantity}).",
                        product.Id, product.Name, product.StockQuantity);

                    // Dynamic, live instant message alert
                    await _notifier.SendInstantWebhookAlertAsync(
                        title: "Critical Stock Depletion",
                        message: $"Product '{product.Name}' (ID: {product.Id}) dropped to {product.StockQuantity} items during POS sync.",
                        severity: "Critical"
                    );
                }
            }
            else
            {
                _logger.LogError("Inventory Error: Sync payload requested deduction for non-existent Product ID {Id}.", deduction.ProductId);
            }
        }

        // 4. Persist the changes back to the SQL Server context
        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully updated inventory levels for {Count} unique products.", products.Count);
    }
}