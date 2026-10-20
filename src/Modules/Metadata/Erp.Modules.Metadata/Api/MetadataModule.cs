using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Modules;
using Erp.Infrastructure.Abstractions.Tenancy;
using Erp.Modules.Metadata.Application;
using Erp.Modules.Metadata.Domain;
using Erp.Modules.Metadata.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Metadata.Api;

public class MetadataModule : IModule
{
    public string Name => "Metadata";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MetadataDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Metadata", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IMetadataService, MetadataService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
