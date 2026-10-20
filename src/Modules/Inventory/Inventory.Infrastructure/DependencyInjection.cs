using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Inventory.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Inventory", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());
        return services;
    }
}
