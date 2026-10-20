using Erp.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Abstractions.Data;

public static class ConcurrencyConfiguration
{
    /// <summary>
    /// Configures timestamp-based optimistic concurrency token for an entity
    /// </summary>
    public static PropertyBuilder<DateTime> ConfigureConcurrencyTimestamp<T>(this EntityTypeBuilder<T> builder)
        where T : class, IVersionedEntity
    {
        return builder.Property(x => x.ConcurrencyTimestamp)
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .IsConcurrencyToken()
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAddOrUpdate();
    }

    /// <summary>
    /// Applies timestamp-based concurrency token configuration to all entities in the ModelBuilder implementing IVersionedEntity
    /// </summary>
    public static void ApplyConcurrencyConfiguration(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IVersionedEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(nameof(IVersionedEntity.ConcurrencyTimestamp))
                    .HasColumnType("timestamp with time zone")
                    .IsRequired()
                    .IsConcurrencyToken()
                    .HasDefaultValueSql("CURRENT_TIMESTAMP")
                    .ValueGeneratedOnAddOrUpdate();
            }
        }
    }

    /// <summary>
    /// PostgreSQL trigger function to automatically refresh ConcurrencyTimestamp on UPDATE
    /// </summary>
    public const string PostgresTriggerFunctionSql = """
        CREATE OR REPLACE FUNCTION update_concurrency_timestamp()
        RETURNS TRIGGER AS $$
        BEGIN
            NEW."ConcurrencyTimestamp" = CURRENT_TIMESTAMP;
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;
        """;

    /// <summary>
    /// Generates SQL to create the PostgreSQL BEFORE UPDATE trigger for a given table
    /// </summary>
    public static string CreatePostgresTriggerSql(string tableName, string schema = "public") => $"""
        CREATE OR REPLACE TRIGGER trg_{tableName}_concurrency
        BEFORE UPDATE ON "{schema}"."{tableName}"
        FOR EACH ROW
        EXECUTE FUNCTION update_concurrency_timestamp();
        """;
}
