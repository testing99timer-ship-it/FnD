using Microsoft.EntityFrameworkCore;
using FnD.Cloud.API.Data;
using FnD.Cloud.API.Models;     // Added to reference CloudOrder
using FnD.Shared.DTOs;         // Added to reference your shared package

var builder = WebApplication.CreateBuilder(args);

// Hook up SQL Server
builder.Services.AddDbContext<CloudDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
// NEW: THE OFFLINE SYNC ENDPOINT
// ==========================================
app.MapPost("/api/sync/orders", async (
    List<FnD.Cloud.API.Models.CloudOrder> incomingOrders,
    FnD.Cloud.API.Data.CloudDbContext dbContext,
    FnD.Cloud.API.Services.SyncAuditService auditService,
    FnD.Cloud.API.Services.InventoryService inventoryService) => // <-- Injected here
{
    if (incomingOrders == null || !incomingOrders.Any())
    {
        await auditService.LogSyncActivityAsync("Local_POS_Terminal", 0, 0, "Warning", "Empty payload sent.");
        return Results.BadRequest("No orders provided for synchronization.");
    }

    try
    {
        var incomingLocalIds = incomingOrders.Select(o => o.LocalOrderId).ToList();

        var existingLocalIds = await dbContext.Orders
            .Where(o => incomingLocalIds.Contains(o.LocalOrderId))
            .Select(o => o.LocalOrderId)
            .ToListAsync();

        // Isolate purely brand new orders
        var newOrders = incomingOrders
            .Where(o => !existingLocalIds.Contains(o.LocalOrderId))
            .ToList();

        if (newOrders.Any())
        {
            await dbContext.Orders.AddRangeAsync(newOrders);
            await dbContext.SaveChangesAsync();

            // ==========================================
            // DEDUCT INVENTORY FOR NEW SYNCED ENTRIES ONLY
            // ==========================================
            await inventoryService.ProcessInventoryDeductionsAsync(newOrders);
        }

        var allSyncedIds = existingLocalIds.Concat(newOrders.Select(o => o.LocalOrderId)).ToList();

        await auditService.LogSyncActivityAsync(
            clientMachine: "Local_POS_Terminal_01",
            processedCount: newOrders.Count,
            duplicateCount: existingLocalIds.Count,
            status: "Success"
        );

        return Results.Ok(new { Message = "Sync completed cleanly.", SyncedLocalIds = allSyncedIds });
    }
    catch (Exception ex)
    {
        await auditService.LogSyncActivityAsync("Local_POS_Terminal_01", 0, 0, "Failed", ex.Message);
        var notifier = app.Services.GetRequiredService<FnD.Cloud.API.Services.NotificationService>();
        await notifier.SendInstantWebhookAlertAsync("POS Sync Pipeline Crash", $"Exception Details: {ex.Message}", "Critical");
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

app.MapGet("/api/reports/dashboard", async (FnD.Cloud.API.Services.DashboardReportingService reportingService) =>
{
    try
    {
        var dashboardData = await reportingService.GetLiveManagementDashboardAsync();
        return Results.Ok(dashboardData);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to compile dashboard metrics: {ex.Message}");
    }
})
.WithName("GetDashboardMetrics");
// ==========================================

app.Run();

// Simple Data Transfer Object (DTO) at the bottom of Program.cs or in your Models folder
public record ContextQuestionDto(string Question);