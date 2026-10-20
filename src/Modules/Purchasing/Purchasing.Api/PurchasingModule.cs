using Erp.Infrastructure.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purchasing.Application.Features.PurchaseOrders.Commands;

namespace Purchasing.Api;

public class PurchasingModule : IModule
{
    public string Name => "Purchasing";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreatePurchaseOrderCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints migrated to ASP.NET Core API Controllers (PurchaseOrdersController)
    }
}
