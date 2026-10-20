using Erp.Infrastructure.Abstractions.Modules;
using Inventory.Application.Features.Products.Commands;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Api;

public class InventoryModule : IModule
{
    public string Name => "Inventory";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints migrated to ASP.NET Core API Controllers (ProductsController)
    }
}
