using Microsoft.Data.Sqlite;
using Dapper;

namespace FnD.Client.Infrastructure.Data;

public class LocalDbInitializer
{
    private readonly string _connectionString;

    public LocalDbInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Create local tables matching our cloud structures, plus an IsSynced flag for offline tracking
        string createTablesSql = @"
            CREATE TABLE IF NOT EXISTS LocalProducts (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                StockQuantity INTEGER NOT NULL,
                Price REAL NOT NULL
            );

            CREATE TABLE IF NOT EXISTS LocalOrders (
                LocalOrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                OrderDate TEXT NOT NULL,
                TotalAmount REAL NOT NULL,
                IsSynced INTEGER DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS LocalOrderItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                LocalOrderId INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Quantity INTEGER NOT NULL,
                UnitPrice REAL NOT NULL,
                FOREIGN KEY(LocalOrderId) REFERENCES LocalOrders(LocalOrderId)
            );";

        connection.Execute(createTablesSql);
    }
}