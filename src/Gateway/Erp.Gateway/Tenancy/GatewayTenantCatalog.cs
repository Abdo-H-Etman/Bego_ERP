using System.Collections.Concurrent;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;

namespace Erp.Gateway.Tenancy;

public class GatewayTenantCatalog : ITenantCatalog
{
    private static readonly ConcurrentDictionary<Guid, TenantContext> ById = new();
    private static readonly ConcurrentDictionary<string, TenantContext> ByAppId = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, TenantContext> ByLegal = new(StringComparer.OrdinalIgnoreCase);

    static GatewayTenantCatalog()
    {
        // Seed demo tenants for out-of-the-box gateway testing
        var t1 = new TenantContext(Guid.Parse("11111111-1111-1111-1111-111111111111"), "EG", "shared-s1");
        var t2 = new TenantContext(Guid.Parse("22222222-2222-2222-2222-222222222222"), "SA", "shared-s1");
        var t3 = new TenantContext(Guid.Parse("33333333-3333-3333-3333-333333333333"), "AE", "dedicated-acme");

        ById[t1.TenantId] = t1;
        ByAppId["acme-eg"] = t1;
        ByLegal["EG:123456789"] = t1;

        ById[t2.TenantId] = t2;
        ByAppId["acme-sa"] = t2;
        ByLegal["SA:987654321"] = t2;

        ById[t3.TenantId] = t3;
        ByAppId["acme-ae"] = t3;
        ByLegal["AE:555555555"] = t3;
    }

    public Task<TenantContext?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        ById.TryGetValue(tenantId, out var context);
        return Task.FromResult(context);
    }

    public Task<TenantContext?> ResolveByAppIdAsync(string appId, CancellationToken cancellationToken = default)
    {
        ByAppId.TryGetValue(appId, out var context);
        return Task.FromResult(context);
    }

    public Task<TenantContext?> ResolveByLegalIdAsync(string legalId, string countryCode, CancellationToken cancellationToken = default)
    {
        var key = $"{countryCode.Trim().ToUpperInvariant()}:{legalId.Trim()}";
        ByLegal.TryGetValue(key, out var context);
        return Task.FromResult(context);
    }

    public Task<IReadOnlyList<TenantContext>> GetAllTenantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TenantContext>>(ById.Values.ToList());
    }

    public string? GetDedicatedConnection(Guid tenantId, string module) => null;
}
