namespace Erp.SharedKernel;

public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; set; }
    string? CreatedBy { get; set; }
    DateTime? ModifiedAtUtc { get; set; }
    string? ModifiedBy { get; set; }
}

public interface IVersionedEntity
{
    /// <summary>
    /// Concurrency token mapped to PostgreSQL timestamp with time zone
    /// </summary>
    DateTime ConcurrencyTimestamp { get; set; }
}

public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAtUtc { get; set; }
}
