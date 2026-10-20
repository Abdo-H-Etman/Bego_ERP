using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Modules;
using Erp.Infrastructure.Abstractions.Tenancy;
using Erp.Modules.Identity.Application;
using Erp.Modules.Identity.Domain;
using Erp.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Identity.Api;

public class IdentityModule : IModule
{
    public string Name => "Identity";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Identity", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
