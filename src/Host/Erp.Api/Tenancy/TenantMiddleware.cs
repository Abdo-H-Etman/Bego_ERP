using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Tenancy;

public class TenantMiddleware(RequestDelegate next, ILogger logger)
{
    private readonly ILogger _logger = logger.ForContext<TenantMiddleware>();

    public async Task InvokeAsync(HttpContext context, TenantContextAccessor tenantAccessor)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdStr) &&
            Guid.TryParse(tenantIdStr, out var tenantId))
        {
            var country = context.Request.Headers["X-Tenant-Country"].FirstOrDefault() ?? "EG";
            var shardKey = context.Request.Headers["X-Tenant-ShardKey"].FirstOrDefault() ?? "shared-s1";

            var tenantContext = new TenantContext(tenantId, country, shardKey);
            tenantAccessor.SetTenant(tenantContext);

            _logger.Debug("TenantContext populated: TenantId={TenantId}, Country={Country}, Shard={ShardKey}",
                tenantId, country, shardKey);
        }

        await next(context);
    }
}
