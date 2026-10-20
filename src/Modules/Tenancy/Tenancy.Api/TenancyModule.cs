using Erp.Infrastructure.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenancy.Application.Features.Tenants.Commands;

namespace Tenancy.Api;

public class TenancyModule : IModule
{
    public string Name => "Tenancy";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // Register MediatR handlers from Tenancy.Application
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(RegisterTenantCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints migrated to ASP.NET Core API Controllers (TenantsController)
    }
}
