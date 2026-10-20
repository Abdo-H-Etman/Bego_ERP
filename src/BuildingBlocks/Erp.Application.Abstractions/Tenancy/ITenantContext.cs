namespace Erp.Application.Abstractions.Tenancy;

public record TenantContext(Guid TenantId, string Country, string ShardKey)
{
    public static readonly TenantContext Empty = new(Guid.Empty, string.Empty, string.Empty);
}

public interface ITenantContext
{
    Guid TenantId { get; }
    string Country { get; }
    string ShardKey { get; }
    TenantContext Current { get; }
}
