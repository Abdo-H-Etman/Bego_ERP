using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.Extensions.Configuration;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Data;

public class TenantConnectionResolver(
    IConfiguration configuration,
    ITenantCatalog catalog,
    ILogger logger) : ITenantConnectionResolver
{
    private readonly ILogger _logger = logger.ForContext<TenantConnectionResolver>();

    public string Resolve(string module, TenantContext tenant)
    {
        // 1. Dedicated DB for this tenant?
        if (tenant.TenantId != Guid.Empty)
        {
            var custom = catalog.GetDedicatedConnection(tenant.TenantId, module);
            if (!string.IsNullOrWhiteSpace(custom))
            {
                _logger.Debug("Using dedicated connection for Tenant {TenantId} on module {Module}", tenant.TenantId, module);
                return custom;
            }
        }

        // 2. Otherwise shard-specific database for this module
        if (!string.IsNullOrWhiteSpace(tenant.ShardKey))
        {
            var shardConnection = configuration[$"Shards:{tenant.ShardKey}:{module}"];
            if (!string.IsNullOrWhiteSpace(shardConnection))
            {
                _logger.Debug("Using shard {ShardKey} connection for module {Module}", tenant.ShardKey, module);
                return shardConnection;
            }
        }

        // 3. Fallback to default module connection string
        var defaultModuleConn = configuration.GetConnectionString(module);
        if (!string.IsNullOrWhiteSpace(defaultModuleConn))
        {
            return defaultModuleConn;
        }

        // 4. Default localized connection string
        var fallback = $"Host=localhost;Database=erp_{module.ToLowerInvariant()};Username=postgres;Password=postgres";
        return fallback;
    }
}
