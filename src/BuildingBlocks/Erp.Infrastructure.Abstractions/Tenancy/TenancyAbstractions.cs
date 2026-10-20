using Erp.Application.Abstractions.Tenancy;

namespace Erp.Infrastructure.Abstractions.Tenancy;

public interface ITenantCatalog
{
    Task<TenantContext?> GetTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantContext?> ResolveByAppIdAsync(string appId, CancellationToken cancellationToken = default);
    Task<TenantContext?> ResolveByLegalIdAsync(string legalId, string countryCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantContext>> GetAllTenantsAsync(CancellationToken cancellationToken = default);
    string? GetDedicatedConnection(Guid tenantId, string module);
}

public interface ITenantConnectionResolver
{
    string Resolve(string module, TenantContext tenant);
}
