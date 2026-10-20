using System.Collections.Concurrent;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Tenancy.Domain;

namespace Tenancy.Infrastructure;

public class TenantCatalogService(TenancyDbContext dbContext) : ITenantCatalog
{
    private static readonly ConcurrentDictionary<Guid, TenantContext> IdCache = new();
    private static readonly ConcurrentDictionary<string, TenantContext> AppIdCache = new();
    private static readonly ConcurrentDictionary<string, TenantContext> LegalIdCache = new();
    private static readonly ConcurrentDictionary<string, string> DedicatedConnections = new();

    public async Task<TenantContext?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (IdCache.TryGetValue(tenantId, out var cached))
            return cached;

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant is null) return null;

        var context = ToContext(tenant);
        CacheTenant(tenant, context);
        return context;
    }

    public async Task<TenantContext?> ResolveByAppIdAsync(string appId, CancellationToken cancellationToken = default)
    {
        var key = appId.Trim().ToLowerInvariant();
        if (AppIdCache.TryGetValue(key, out var cached))
            return cached;

        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.AppId == appId && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant is null) return null;

        var context = ToContext(tenant);
        CacheTenant(tenant, context);
        return context;
    }

    public async Task<TenantContext?> ResolveByLegalIdAsync(string legalId, string countryCode, CancellationToken cancellationToken = default)
    {
        var key = $"{countryCode.Trim().ToUpperInvariant()}:{legalId.Trim().ToLowerInvariant()}";
        if (LegalIdCache.TryGetValue(key, out var cached))
            return cached;

        var upperCountry = countryCode.Trim().ToUpperInvariant();
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.LegalId == legalId && t.CountryCode == upperCountry && t.Status == TenantStatus.Active, cancellationToken);

        if (tenant is null) return null;

        var context = ToContext(tenant);
        CacheTenant(tenant, context);
        return context;
    }

    public async Task<IReadOnlyList<TenantContext>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Status == TenantStatus.Active)
            .ToListAsync(cancellationToken);

        return tenants.Select(ToContext).ToList();
    }

    public string? GetDedicatedConnection(Guid tenantId, string module)
    {
        var key = $"{tenantId}:{module}";
        if (DedicatedConnections.TryGetValue(key, out var conn))
            return conn;

        var tenant = dbContext.Tenants.AsNoTracking().FirstOrDefault(t => t.Id == tenantId);
        if (tenant?.CustomConnectionString != null)
        {
            DedicatedConnections[key] = tenant.CustomConnectionString;
            return tenant.CustomConnectionString;
        }

        return null;
    }

    private static TenantContext ToContext(Tenant tenant) =>
        new(tenant.Id, tenant.CountryCode, tenant.ShardKey);

    private static void CacheTenant(Tenant tenant, TenantContext context)
    {
        IdCache[tenant.Id] = context;
        if (!string.IsNullOrWhiteSpace(tenant.AppId))
            AppIdCache[tenant.AppId.ToLowerInvariant()] = context;
        if (!string.IsNullOrWhiteSpace(tenant.LegalId))
            LegalIdCache[$"{tenant.CountryCode}:{tenant.LegalId.ToLowerInvariant()}"] = context;
        if (!string.IsNullOrWhiteSpace(tenant.CustomConnectionString))
            DedicatedConnections[tenant.Id.ToString()] = tenant.CustomConnectionString;
    }
}
