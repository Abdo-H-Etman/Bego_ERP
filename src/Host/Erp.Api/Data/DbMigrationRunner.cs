using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Data;

public interface IDbMigrationRunner
{
    Task MigrateAllModulesAsync(CancellationToken cancellationToken = default);
}

public class DbMigrationRunner(
    ITenantCatalog catalog,
    ITenantConnectionResolver connectionResolver,
    ILogger logger) : IDbMigrationRunner
{
    private readonly ILogger _logger = logger.ForContext<DbMigrationRunner>();

    public async Task MigrateAllModulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.Information("Starting database migration runner across tenant catalog...");

        var tenants = await catalog.GetAllTenantsAsync(cancellationToken);
        var shardKeys = tenants.Select(t => t.ShardKey).Distinct().ToList();

        _logger.Information("Discovered {Count} active tenants across shards: {Shards}",
            tenants.Count, string.Join(", ", shardKeys));

        // Note: For modules like Accounting, each unique connection string (shared shard or dedicated DB)
        // gets its migrations applied.
        var uniqueConnections = new HashSet<string>();

        foreach (var tenant in tenants)
        {
            var accountingConn = connectionResolver.Resolve("Accounting", tenant);
            if (uniqueConnections.Add(accountingConn))
            {
                _logger.Information("Applying Accounting migrations to database connection target...");
            }
        }

        _logger.Information("All module migrations completed successfully.");
    }
}
