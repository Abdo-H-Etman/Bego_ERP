using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sales.Application;

namespace Sales.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SalesDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Sales", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<ISalesDbContext>(sp => sp.GetRequiredService<SalesDbContext>());
        return services;
    }
}
