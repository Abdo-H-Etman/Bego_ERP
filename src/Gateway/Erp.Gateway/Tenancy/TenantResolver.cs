using System.Security.Claims;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using ILogger = Serilog.ILogger;

namespace Erp.Gateway.Tenancy;

public interface ITenantResolver
{
    Task<TenantContext?> ResolveAsync(HttpContext context);
}

public class TenantResolver(
    ITenantCatalog catalog,
    ILogger logger) : ITenantResolver
{
    private readonly ILogger _logger = logger.ForContext<TenantResolver>();

    public async Task<TenantContext?> ResolveAsync(HttpContext context)
    {
        // 1. JWT Claims
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value
                ?? context.User.FindFirst("TenantId")?.Value;

            if (Guid.TryParse(tenantIdClaim, out var tenantGuid))
            {
                var tenant = await catalog.GetTenantAsync(tenantGuid);
                if (tenant != null) return tenant;
            }

            var claimAppId = context.User.FindFirst("app_id")?.Value;
            if (!string.IsNullOrWhiteSpace(claimAppId))
            {
                var tenant = await catalog.ResolveByAppIdAsync(claimAppId);
                if (tenant != null) return tenant;
            }

            var claimLegalId = context.User.FindFirst("legal_id")?.Value;
            var claimCountry = context.User.FindFirst("country")?.Value;
            if (!string.IsNullOrWhiteSpace(claimLegalId) && !string.IsNullOrWhiteSpace(claimCountry))
            {
                var tenant = await catalog.ResolveByLegalIdAsync(claimLegalId, claimCountry);
                if (tenant != null) return tenant;
            }
        }

        // 2. Request Headers
        if (context.Request.Headers.TryGetValue("X-App-Id", out var headerAppId) && !string.IsNullOrWhiteSpace(headerAppId))
        {
            var tenant = await catalog.ResolveByAppIdAsync(headerAppId.ToString());
            if (tenant != null) return tenant;
        }

        if (context.Request.Headers.TryGetValue("X-Legal-Id", out var headerLegalId) &&
            context.Request.Headers.TryGetValue("X-Country", out var headerCountry) &&
            !string.IsNullOrWhiteSpace(headerLegalId) && !string.IsNullOrWhiteSpace(headerCountry))
        {
            var tenant = await catalog.ResolveByLegalIdAsync(headerLegalId.ToString(), headerCountry.ToString());
            if (tenant != null) return tenant;
        }

        // 3. Subdomain (e.g. acme-eg.erp.com -> acme-eg)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var potentialSubdomain = parts[0];
            if (!string.Equals(potentialSubdomain, "api", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(potentialSubdomain, "www", StringComparison.OrdinalIgnoreCase))
            {
                var tenant = await catalog.ResolveByAppIdAsync(potentialSubdomain);
                if (tenant != null) return tenant;
            }
        }

        _logger.Debug("No tenant resolved for path: {Path}", context.Request.Path);
        return null;
    }
}
