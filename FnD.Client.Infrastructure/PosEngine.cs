using FnD.Client.Infrastructure.Data;
using FnD.Client.Infrastructure.Repositories;
using FnD.Client.Infrastructure.Sync;

namespace FnD.Client.Infrastructure;

public class PosEngine
{
    private readonly string _connectionString;
    private CancellationTokenSource? _syncCts;

    public LocalOrderRepository Orders { get; }
    public LocalSyncWorker SyncWorker { get; }

    public PosEngine(string dbPath, HttpClient httpClient, string gatewayUrl)
    {
        _connectionString = $"Data Source={dbPath};";

        var initializer = new LocalDbInitializer(_connectionString);
        initializer.InitializeDatabase();

        Orders = new LocalOrderRepository(_connectionString);
        SyncWorker = new LocalSyncWorker(_connectionString, httpClient, gatewayUrl);
    }

    /// <summary>
    /// Starts a headless, non-blocking background synchronization loop.
    /// Hand this method off to your frontend partner to invoke inside their Avalonia startup pipeline.
    /// </summary>
    public void StartBackgroundSyncProcessor(TimeSpan interval)
    {
        _syncCts = new CancellationTokenSource();
        var token = _syncCts.Token;

        // Run the timer loop entirely on a background thread pool worker
        Task.Run(async () =>
        {
            using var periodicTimer = new PeriodicTimer(interval);

            // Trigger an immediate sync cycle on startup
            await SyncWorker.RunSyncCycleAsync();

            // Wait smoothly for the next interval pulse without thread starvation
            while (await periodicTimer.WaitForNextTickAsync(token) && !token.IsCancellationRequested)
            {
                await SyncWorker.RunSyncCycleAsync();
            }
        }, token);
    }

    /// <summary>
    /// Safely terminates the background sync process when the desktop application is shutting down.
    /// </summary>
    public void StopBackgroundSyncProcessor()
    {
        _syncCts?.Cancel();
        _syncCts?.Dispose();
    }
}