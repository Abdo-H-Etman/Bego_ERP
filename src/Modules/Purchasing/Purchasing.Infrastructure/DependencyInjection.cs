using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Purchasing.Application;

namespace Purchasing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPurchasingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PurchasingDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Purchasing", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IPurchasingDbContext>(sp => sp.GetRequiredService<PurchasingDbContext>());
        return services;
    }
}
