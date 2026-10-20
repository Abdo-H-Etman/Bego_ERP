namespace Erp.Infrastructure.Abstractions.Data;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string? UserId { get; set; }
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string KeyValues { get; set; } = "{}";
    public string OldValues { get; set; } = "{}";
    public string NewValues { get; set; } = "{}";
    public string AffectedColumns { get; set; } = "[]";
}
