using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Dapper;
using FnD.Shared.DTOs;

namespace FnD.Client.Infrastructure.Sync;

public class LocalSyncWorker
{
    private readonly string _connectionString;
    private readonly HttpClient _httpClient;
    private readonly string _gatewayUrl;

    public LocalSyncWorker(string connectionString, HttpClient httpClient, string gatewayUrl)
    {
        _connectionString = connectionString;
        _httpClient = httpClient;
        // Point this to your YARP Gateway port (e.g., "http://localhost:5000")
        _gatewayUrl = gatewayUrl.TrimEnd('/');
    }

    /// <summary>
    /// Sweeps the local SQLite database for un-synchronized transactions and posts them to the Cloud Gateway.
    /// </summary>
    public async Task RunSyncCycleAsync()
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // 1. Fetch all local orders that haven't been pushed to the cloud yet
        string pendingOrdersSql = "SELECT LocalOrderId, OrderDate, TotalAmount FROM LocalOrders WHERE IsSynced = 0;";
        var pendingOrders = (await connection.QueryAsync<dynamic>(pendingOrdersSql)).ToList();

        if (!pendingOrders.Any()) return; // Nothing to sync!

        var payload = new List<OrderSyncDto>();

        foreach (var order in pendingOrders)
        {
            // 2. Fetch the corresponding items for this specific pending order
            string itemsSql = "SELECT ProductId, Quantity, UnitPrice FROM LocalOrderItems WHERE LocalOrderId = @LocalOrderId;";
            var items = (await connection.QueryAsync<dynamic>(itemsSql, new { LocalOrderId = order.LocalOrderId })).ToList();

            // 3. Map the flat SQLite rows directly into your shared structural DTO
            var dto = new OrderSyncDto
            {
                LocalOrderId = (int)order.LocalOrderId,
                OrderDate = DateTime.Parse(order.OrderDate),
                TotalAmount = (decimal)order.TotalAmount,
                Items = items.Select(i => new OrderItemSyncDto
                {
                    ProductId = (int)i.ProductId,
                    Quantity = (int)i.Quantity,
                    UnitPrice = (decimal)i.UnitPrice
                }).ToList()
            };

            payload.Add(dto);
        }

        try
        {
            // Clear any existing headers to prevent stacking, then apply your firm's unique Tenant ID
            _httpClient.DefaultRequestHeaders.Remove("X-Tenant-Id");
            _httpClient.DefaultRequestHeaders.Add("X-Tenant-Id", "TENANT_STORE_DELHI_01"); // This value will come from app config later

            // 4. Ship the batch data over the wire to the unified YARP Gateway endpoint
            var response = await _httpClient.PostAsJsonAsync($"{_gatewayUrl}/api/sync/orders", payload);

            if (response.IsSuccessStatusCode)
            {
                // 5. If the cloud accepts it, update local states so we don't double-sync next time
                string updateSyncStatusSql = "UPDATE LocalOrders SET IsSynced = 1 WHERE LocalOrderId = @LocalOrderId;";

                foreach (var order in pendingOrders)
                {
                    await connection.ExecuteAsync(updateSyncStatusSql, new { LocalOrderId = order.LocalOrderId });
                }
            }
        }
        catch (Exception)
        {
            // Network is transiently down or gateway is offline. 
            // Fail silently; the next sync cycle will catch it automatically when connection restores.
        }
    }
}