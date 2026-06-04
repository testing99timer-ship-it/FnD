using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FnD.Gateway.API.Middleware;

public class TenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantValidationMiddleware> _logger;

    public TenantValidationMiddleware(RequestDelegate next, ILogger<TenantValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 🔥 FIX: Bypass tenant verification for browser CORS preflight (OPTIONS) requests
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 1. Extract the Tenant ID from custom HTTP Headers
        if (!context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantId) || string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Blocked unauthenticated request: Missing 'X-Tenant-Id' header. Path: {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Access Denied: Missing Tenant Identification Context." });
            return;
        }

        // 2. Validate the Tenant (Mocking a database lookup/cache check for now)
        if (tenantId == "MALICIOUS_STORE_ID")
        {
            _logger.LogCritical("Security Alert: Blocked blacklisted Tenant ID: {TenantId}", tenantId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Access Forbidden: Invalid or revoked Tenant ID." });
            return;
        }

        // 3. Append tenant tracking context into the request pipeline log headers
        _logger.LogInformation("Routing verified request for Tenant: {TenantId} -> {Path}", tenantId, context.Request.Path);

        // Pass the request to the next component in the pipeline (YARP Proxy)
        await _next(context);
    }
}