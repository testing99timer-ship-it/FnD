using Microsoft.Data.Sqlite;
using Dapper;
using FnD.Shared.DTOs; // This assumes you have an OrderSyncDto or shared order model

namespace FnD.Client.Infrastructure.Repositories;

public class LocalOrderRepository
{
    private readonly string _connectionString;

    public LocalOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Saves a complete client order locally into SQLite immediately using high-speed Dapper transactions.
    /// </summary>
    public bool SaveOrderToLocalCache(decimal totalAmount, List<TopProductDto> items)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Insert parent order entry
            string insertOrderSql = @"
                INSERT INTO LocalOrders (OrderDate, TotalAmount, IsSynced) 
                VALUES (@OrderDate, @TotalAmount, 0);
                SELECT last_insert_rowid();";

            var localOrderId = connection.ExecuteScalar<int>(insertOrderSql, new
            {
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalAmount = totalAmount
            }, transaction);

            // 2. Insert nested child items
            string insertItemSql = @"
                INSERT INTO LocalOrderItems (LocalOrderId, ProductId, Quantity, UnitPrice) 
                VALUES (@LocalOrderId, @ProductId, @Quantity, @UnitPrice);";

            foreach (var item in items)
            {
                connection.Execute(insertItemSql, new
                {
                    LocalOrderId = localOrderId,
                    ProductId = item.ProductId,
                    Quantity = item.TotalQuantitySold, // Map from your shared contract fields
                    UnitPrice = item.TotalRevenueGenerated / (item.TotalQuantitySold == 0 ? 1 : item.TotalQuantitySold)
                }, transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}