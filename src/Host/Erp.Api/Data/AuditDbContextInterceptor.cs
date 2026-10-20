using System.Text.Json;
using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Data;
using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ILogger = Serilog.ILogger;

namespace Erp.Api.Data;

public class AuditDbContextInterceptor(ITenantContext tenantContext, ILogger logger) : SaveChangesInterceptor
{
    private readonly ILogger _logger = logger.ForContext<AuditDbContextInterceptor>();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        UpdateAuditableEntities(eventData.Context);
        CaptureAuditTrail(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext context)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
                entry.Entity.CreatedBy ??= $"tenant:{tenantContext.TenantId}";
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAtUtc = now;
                entry.Entity.ModifiedBy ??= $"tenant:{tenantContext.TenantId}";
            }
        }
    }

    private void CaptureAuditTrail(DbContext context)
    {
        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var tenantId = tenantContext.TenantId;
        var userId = tenantId != Guid.Empty ? $"tenant:{tenantId}" : "system";

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue;

            var entityName = entry.Metadata.ClrType.Name;
            var action = entry.State.ToString();

            var keyValues = new Dictionary<string, object?>();
            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var affectedColumns = new List<string>();

            foreach (var prop in entry.Properties)
            {
                var propName = prop.Metadata.Name;
                if (prop.Metadata.IsPrimaryKey())
                {
                    keyValues[propName] = prop.CurrentValue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propName] = prop.CurrentValue;
                        affectedColumns.Add(propName);
                        break;

                    case EntityState.Deleted:
                        oldValues[propName] = prop.OriginalValue;
                        affectedColumns.Add(propName);
                        break;

                    case EntityState.Modified:
                        if (prop.IsModified)
                        {
                            affectedColumns.Add(propName);
                            oldValues[propName] = prop.OriginalValue;
                            newValues[propName] = prop.CurrentValue;
                        }
                        break;
                }
            }

            var auditLog = new AuditLog
            {
                TenantId = tenantId,
                UserId = userId,
                TableName = entityName,
                Action = action,
                TimestampUtc = DateTime.UtcNow,
                KeyValues = JsonSerializer.Serialize(keyValues),
                OldValues = JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues),
                AffectedColumns = JsonSerializer.Serialize(affectedColumns)
            };

            _logger.Information(
                "AUDIT TRAIL [{Action}] on Entity: {TableName} | Keys: {KeyValues} | Changed: {AffectedColumns} | By: {UserId} | Tenant: {TenantId} | Old: {OldValues} | New: {NewValues}",
                auditLog.Action, auditLog.TableName, auditLog.KeyValues, auditLog.AffectedColumns, auditLog.UserId, auditLog.TenantId, auditLog.OldValues, auditLog.NewValues);
        }
    }
}
