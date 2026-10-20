using Erp.SharedKernel;

namespace Tenancy.Domain;

public enum TenantStatus
{
    Active,
    Suspended,
    PendingActivation
}

public sealed class Tenant : AggregateRoot<Guid>
{
    private Tenant() { } // EF Core

    private Tenant(
        Guid id,
        string? appId,
        string? legalId,
        string countryCode,
        TenantStatus status,
        string shardKey,
        string? customConnectionString)
        : base(id)
    {
        AppId = appId;
        LegalId = legalId;
        CountryCode = countryCode.ToUpperInvariant();
        Status = status;
        ShardKey = shardKey;
        CustomConnectionString = customConnectionString;
    }

    public string? AppId { get; private set; }
    public string? LegalId { get; private set; }
    public string CountryCode { get; private set; } = string.Empty;
    public TenantStatus Status { get; private set; }
    public string ShardKey { get; private set; } = string.Empty;
    public string? CustomConnectionString { get; private set; }

    public static Result<Tenant> Create(
        string? appId,
        string? legalId,
        string countryCode,
        string shardKey,
        string? customConnectionString = null)
    {
        if (string.IsNullOrWhiteSpace(appId) && string.IsNullOrWhiteSpace(legalId))
        {
            return Result.Failure<Tenant>(new Error("Tenant.IdentifierRequired", "Either AppId or LegalId must be provided."));
        }

        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Trim().Length != 2)
        {
            return Result.Failure<Tenant>(new Error("Tenant.InvalidCountry", "CountryCode must be a valid 2-letter ISO code."));
        }

        if (string.IsNullOrWhiteSpace(shardKey))
        {
            shardKey = "shared-s1";
        }

        var tenant = new Tenant(
            Guid.NewGuid(),
            string.IsNullOrWhiteSpace(appId) ? null : appId.Trim(),
            string.IsNullOrWhiteSpace(legalId) ? null : legalId.Trim(),
            countryCode.Trim(),
            TenantStatus.Active,
            shardKey.Trim(),
            customConnectionString);

        tenant.AddDomainEvent(new TenantRegisteredDomainEvent(tenant.Id, tenant.AppId, tenant.CountryCode));

        return tenant;
    }

    public void UpdateStatus(TenantStatus newStatus)
    {
        Status = newStatus;
    }

    public void SetDedicatedConnection(string connectionString)
    {
        CustomConnectionString = connectionString;
    }
}

public record TenantRegisteredDomainEvent(Guid TenantId, string? AppId, string CountryCode) : DomainEvent;
