using Microsoft.EntityFrameworkCore;
using FnD.Cloud.API.Data;
using FnD.Cloud.API.Models;
using FnD.Shared.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Hook up SQL Server
builder.Services.AddDbContext<CloudDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// NEW: Required to intercept headers forwarded down by the YARP Gateway proxy
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<FnD.Cloud.API.Infrastructure.ITenantProvider, FnD.Cloud.API.Infrastructure.HttpContextTenantProvider>();

builder.Services.AddHttpClient<FnD.Cloud.API.Services.AiReportingService>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
});

builder.Services.AddScoped<FnD.Cloud.API.Services.SyncAuditService>();
builder.Services.AddScoped<FnD.Cloud.API.Services.InventoryService>();
builder.Services.AddHttpClient<FnD.Cloud.API.Services.NotificationService>();
builder.Services.AddScoped<FnD.Cloud.API.Services.DashboardReportingService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ==========================================
// SECURED: THE MULTI-TENANT OFFLINE SYNC ENDPOINT
// ==========================================
app.MapPost("/api/sync/orders", async (
    HttpContext context,
    List<FnD.Cloud.API.Models.CloudOrder> incomingOrders,
    FnD.Cloud.API.Data.CloudDbContext dbContext,
    FnD.Cloud.API.Services.SyncAuditService auditService,
    FnD.Cloud.API.Services.InventoryService inventoryService) =>
{
    // 1. EXTRACT TENANT: Pull the validated header handed down by YARP
    if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) || string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Unauthorized();
    }

    if (incomingOrders == null || !incomingOrders.Any())
    {
        await auditService.LogSyncActivityAsync("Local_POS_Terminal", 0, 0, "Warning", "Empty payload sent.");
        return Results.BadRequest("No orders provided for synchronization.");
    }

    try
    {
        var incomingLocalIds = incomingOrders.Select(o => o.LocalOrderId).ToList();

        // 2. ISOLATED DUPLICATE CHECK: Check duplicates ONLY within this specific store's row boundaries
        var existingLocalIds = await dbContext.Orders
            .Where(o => incomingLocalIds.Contains(o.LocalOrderId) && o.TenantId == tenantId.ToString())
            .Select(o => o.LocalOrderId)
            .ToListAsync();

        // Isolate purely brand new orders for this tenant
        var newOrders = incomingOrders
            .Where(o => !existingLocalIds.Contains(o.LocalOrderId))
            .ToList();

        if (newOrders.Any())
        {
            foreach (var order in newOrders)
            {
                order.TenantId = tenantId.ToString();
            }

            await dbContext.Orders.AddRangeAsync(newOrders);
            await dbContext.SaveChangesAsync();

            // FIX: Pass the entire collection batch safely as a single argument
            await inventoryService.ProcessInventoryDeductionsAsync(newOrders);
        }

        var allSyncedIds = existingLocalIds.Concat(newOrders.Select(o => o.LocalOrderId)).ToList();

        await auditService.LogSyncActivityAsync(
            clientMachine: $"Local_POS_Terminal_{tenantId}",
            processedCount: newOrders.Count,
            duplicateCount: existingLocalIds.Count,
            status: "Success"
        );

        return Results.Ok(new { Message = $"Sync completed cleanly for Tenant {tenantId}.", SyncedLocalIds = allSyncedIds });
    }
    catch (Exception ex)
    {
        await auditService.LogSyncActivityAsync($"Local_POS_Terminal_{tenantId}", 0, 0, "Failed", ex.Message);
        var notifier = app.Services.GetRequiredService<FnD.Cloud.API.Services.NotificationService>();
        await notifier.SendInstantWebhookAlertAsync("POS Sync Pipeline Crash", $"Tenant: {tenantId} | Exception Details: {ex.Message}", "Critical");
        throw;
    }
})
.WithName("SyncOrders");

// ==========================================
app.MapGet("/api/ai/business-summary", async (FnD.Cloud.API.Services.AiReportingService aiService) =>
{
    try
    {
        var summary = await aiService.GetSalesSummaryAsync();
        return Results.Ok(new { Insights = summary });
    }
    catch (Exception ex)
    {
        return Results.Problem($"AI Engine initialization failed: {ex.Message}");
    }
})
.WithName("GetAiSummary");

// ==========================================
app.MapPost("/api/ai/ask", async (ContextQuestionDto request, FnD.Cloud.API.Services.AiReportingService aiService) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest("Question cannot be empty.");
    }

    try
    {
        var answer = await aiService.AskBusinessQuestionAsync(request.Question);
        return Results.Ok(new { Answer = answer });
    }
    catch (Exception ex)
    {
        return Results.Problem($"AI Query Engine failed: {ex.Message}");
    }
})
.WithName("AskAiBrain");

// ==========================================
// SECURED: MANAGEMENT DASHBOARD ENFORCED BY TENANT
// ==========================================
app.MapGet("/api/reports/dashboard", async (
    HttpContext context,
    FnD.Cloud.API.Services.DashboardReportingService reportingService) =>
{
    // Extract tenant header so Store A can never access Store B's metrics
    if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) || string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.Unauthorized();
    }

    try
    {
        // Pass the verified Tenant ID directly down to your financial LINQ join engines
        var dashboardData = await reportingService.GetLiveManagementDashboardAsync(tenantId.ToString());
        return Results.Ok(dashboardData);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to compile dashboard metrics for tenant {tenantId}: {ex.Message}");
    }
})
.WithName("GetDashboardMetrics");

// ==========================================
app.Run();

public record ContextQuestionDto(string Question);