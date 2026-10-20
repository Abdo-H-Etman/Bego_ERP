using Accounting.Application.Features.Invoices.Commands;
using Accounting.Application.Tax;
using Erp.Infrastructure.Abstractions.Modules;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Api;

public class AccountingModule : IModule
{
    public string Name => "Accounting";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ITaxEngineFactory, TaxEngineFactory>();

        // Register MediatR for Accounting.Application
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateInvoiceCommand).Assembly));
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints migrated to ASP.NET Core API Controllers (InvoicesController, TaxController)
    }
}
