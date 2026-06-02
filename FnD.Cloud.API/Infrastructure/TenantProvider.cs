using Microsoft.AspNetCore.Http;

namespace FnD.Cloud.API.Infrastructure;

public interface ITenantProvider
{
    string TenantId { get; }
}

public class HttpContextTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    // Automatically grabs the header forwarded by your YARP Gateway
    public string TenantId =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString()
        ?? throw new InvalidOperationException("Tenant context is missing for this request.");
}