using Erp.Application.Abstractions.Tenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Accounting.Application.Tax;

public class TaxEngineFactory(IServiceProvider serviceProvider, ITenantContext tenantContext) : ITaxEngineFactory
{
    public ITaxEngine GetEngine()
    {
        var country = string.IsNullOrWhiteSpace(tenantContext.Country) ? "EG" : tenantContext.Country.ToUpperInvariant();
        var engine = serviceProvider.GetKeyedService<ITaxEngine>(country);
        if (engine is null)
        {
            // Default fallback to Egypt tax engine
            engine = serviceProvider.GetKeyedService<ITaxEngine>("EG")
                ?? throw new InvalidOperationException($"No tax engine registered for country '{country}'.");
        }

        return engine;
    }
}
