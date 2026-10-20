using Erp.Application.Abstractions.Tenancy;

namespace Erp.Api.Tenancy;

public class TenantContextAccessor : ITenantContext
{
    private TenantContext _current = TenantContext.Empty;

    public Guid TenantId => _current.TenantId;
    public string Country => _current.Country;
    public string ShardKey => _current.ShardKey;
    public TenantContext Current => _current;

    public void SetTenant(TenantContext tenant)
    {
        _current = tenant ?? TenantContext.Empty;
    }
}
