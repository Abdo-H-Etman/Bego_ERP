using Accounting.Application;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccountingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Dynamic DbContext resolution per (module, tenant) as defined in README Section 8
        services.AddDbContext<AccountingDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Accounting", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAccountingDbContext>(sp => sp.GetRequiredService<AccountingDbContext>());
        return services;
    }
}
