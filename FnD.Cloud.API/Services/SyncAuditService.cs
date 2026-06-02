using System;
using System.Threading.Tasks;
using FnD.Cloud.API.Data;
using FnD.Cloud.API.Models;
using Microsoft.Extensions.Logging;

namespace FnD.Cloud.API.Services;

public class SyncAuditService
{
    private readonly CloudDbContext _dbContext;
    private readonly ILogger<SyncAuditService> _logger;

    public SyncAuditService(CloudDbContext dbContext, ILogger<SyncAuditService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogSyncActivityAsync(string clientMachine, int processedCount, int duplicateCount, string status, string? error = null)
    {
        try
        {
            var auditEntry = new SyncLog
            {
                SyncTimestamp = DateTime.UtcNow,
                ClientMachineName = string.IsNullOrWhiteSpace(clientMachine) ? "Local_POS_Terminal" : clientMachine,
                RecordCountProcessed = processedCount,
                DuplicateCountSkipped = duplicateCount,
                Status = status,
                ErrorDetails = error
            };

            await _dbContext.SyncLogs.AddAsync(auditEntry);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Sync Audit Recorded successfully: {Records} processed, {Duplicates} skipped.", processedCount, duplicateCount);
        }
        catch (Exception ex)
        {
            // Fail-safe logging so an audit failure never crashes the actual transactional sync pipeline
            _logger.LogError(ex, "Critical Error: Failed to write synchronization audit logs to SSMS.");
        }
    }
}