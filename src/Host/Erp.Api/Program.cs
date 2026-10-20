using Accounting.Api;
using Accounting.Application.Tax;
using Accounting.Infrastructure;
using Erp.Api.Caching;
using Erp.Api.Data;
using Erp.Api.Messaging;
using Erp.Api.Middleware;
using Erp.Api.Security;
using Erp.Api.Tenancy;
using Erp.Application.Abstractions.Security;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Barcodes;
using Erp.Infrastructure.Abstractions.Caching;
using Erp.Infrastructure.Abstractions.Messaging;
using Erp.Infrastructure.Abstractions.Modules;
using Erp.Infrastructure.Abstractions.Tenancy;
using Erp.Modules.Identity.Api;
using Erp.Modules.Metadata.Api;
using Erp.Modules.Notifications;
using Erp.Modules.Workflow;
using Inventory.Api;
using Inventory.Infrastructure;
using Purchasing.Api;
using Purchasing.Infrastructure;
using Sales.Api;
using Sales.Infrastructure;
using Tax.Egypt;
using Tax.Ksa;
using Tax.Uae;
using Serilog;
using Tenancy.Api;
using Tenancy.Infrastructure;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>{NewLine}{Exception}"));

builder.Services.AddSingleton<Serilog.ILogger>(Log.Logger);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(Tenancy.Api.Controllers.TenantsController).Assembly)
    .AddApplicationPart(typeof(Accounting.Api.Controllers.InvoicesController).Assembly)
    .AddApplicationPart(typeof(Inventory.Api.Controllers.ProductsController).Assembly)
    .AddApplicationPart(typeof(Sales.Api.Controllers.OrdersController).Assembly)
    .AddApplicationPart(typeof(Purchasing.Api.Controllers.PurchaseOrdersController).Assembly)
    .AddApplicationPart(typeof(Erp.Modules.Identity.Controllers.RolesController).Assembly)
    .AddApplicationPart(typeof(Erp.Modules.Metadata.Controllers.MetadataController).Assembly)
    .AddApplicationPart(typeof(Erp.Modules.Workflow.Controllers.WorkflowController).Assembly)
    .AddApplicationPart(typeof(Erp.Modules.Notifications.Controllers.NotificationsController).Assembly);

// Tenancy Context Accessor & Scoped Registration
builder.Services.AddScoped<TenantContextAccessor>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContextAccessor>());

// Connection Resolver & Migration Runner
builder.Services.AddScoped<ITenantConnectionResolver, TenantConnectionResolver>();
builder.Services.AddScoped<IDbMigrationRunner, DbMigrationRunner>();

// Cross-Cutting Infrastructure: Redis Caching, HMAC, Message Broker, Barcodes
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<IHmacService, HmacService>();
builder.Services.AddSingleton<IMessageBroker, InMemoryMessageBroker>();
builder.Services.AddSingleton<IBarcodeGenerator, SvgBarcodeGenerator>();
builder.Services.AddScoped<ILabelRenderer, LabelRenderer>();
builder.Services.AddScoped<AuditDbContextInterceptor>();

// Keyed Tax Engines for Country-specific Localization
builder.Services.AddKeyedScoped<ITaxEngine, EgyptTaxEngine>("EG");
builder.Services.AddKeyedScoped<ITaxEngine, KsaTaxEngine>("SA");
builder.Services.AddKeyedScoped<ITaxEngine, UaeTaxEngine>("AE");

// Module Infrastructure Registration (DbContexts, dynamic connection strings, repositories)
builder.Services.AddTenancyInfrastructure(builder.Configuration);
builder.Services.AddAccountingInfrastructure(builder.Configuration);
builder.Services.AddInventoryInfrastructure(builder.Configuration);
builder.Services.AddSalesInfrastructure(builder.Configuration);
builder.Services.AddPurchasingInfrastructure(builder.Configuration);

// Module Registration as specified in README Section 7
var modules = new IModule[]
{
    new TenancyModule(),
    new AccountingModule(),
    new InventoryModule(),
    new SalesModule(),
    new PurchasingModule(),
    new IdentityModule(),
    new MetadataModule(),
    new WorkflowModule(),
    new NotificationsModule()
};

foreach (var module in modules)
{
    module.Register(builder.Services, builder.Configuration);
}

var app = builder.Build();

app.UseSerilogRequestLogging();

// Global Exception Handling (RFC 7807 ProblemDetails + Concurrency 409 handling)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Tenant resolution middleware
app.UseMiddleware<TenantMiddleware>();

// Begohub ERP HMAC Security Middleware for Data Pulls
app.UseMiddleware<BegohubHmacMiddleware>();

// Map all ASP.NET Core API Controllers
app.MapControllers();

// Map any additional module endpoints
foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();
