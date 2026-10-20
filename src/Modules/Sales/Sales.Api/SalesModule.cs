using Erp.Infrastructure.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application.Features.Orders.Commands;

namespace Sales.Api;

public class SalesModule : IModule
{
    public string Name => "Sales";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints migrated to ASP.NET Core API Controllers (OrdersController)
    }
}
