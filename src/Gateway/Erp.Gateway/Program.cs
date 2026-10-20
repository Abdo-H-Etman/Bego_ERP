using Erp.Gateway.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Serilog;
using Yarp.ReverseProxy.Transforms;

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

// Register Tenancy services for Gateway
builder.Services.AddSingleton<ITenantCatalog, GatewayTenantCatalog>();
builder.Services.AddScoped<ITenantResolver, TenantResolver>();

// Configure YARP Reverse Proxy with Tenant Transform
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(ctx =>
    {
        ctx.AddRequestTransform(async t =>
        {
            var resolver = t.HttpContext.RequestServices.GetRequiredService<ITenantResolver>();
            var tenant = await resolver.ResolveAsync(t.HttpContext);

            // Strip incoming client headers - never trust untrusted client inputs
            t.ProxyRequest.Headers.Remove("X-Tenant-Id");
            t.ProxyRequest.Headers.Remove("X-Tenant-Country");
            t.ProxyRequest.Headers.Remove("X-Tenant-ShardKey");

            if (tenant is not null)
            {
                t.ProxyRequest.Headers.Add("X-Tenant-Id", tenant.TenantId.ToString());
                t.ProxyRequest.Headers.Add("X-Tenant-Country", tenant.Country);
                t.ProxyRequest.Headers.Add("X-Tenant-ShardKey", tenant.ShardKey);
            }
        });
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

app.UseSerilogRequestLogging();

app.UseCors();

// Correlation ID & Logging middleware
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});

app.MapGet("/", () => Results.Ok(new
{
    Gateway = "ERP API Gateway (YARP)",
    Status = "Healthy",
    Version = "1.0.0"
}));

app.MapReverseProxy();

app.Run();
