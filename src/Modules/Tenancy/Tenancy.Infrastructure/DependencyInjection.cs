using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenancy.Application;

namespace Tenancy.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTenancyInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Tenancy")
            ?? "Host=localhost;Database=erp_tenancy;Username=postgres;Password=postgres";

        services.AddDbContext<TenancyDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<ITenancyDbContext>(sp => sp.GetRequiredService<TenancyDbContext>());
        services.AddScoped<ITenantCatalog, TenantCatalogService>();

        return services;
    }
}
