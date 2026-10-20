using Erp.Application.Abstractions.Tenancy;
using Erp.Modules.Metadata.Domain;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Metadata.Infrastructure;

public class MetadataDbContext(
    DbContextOptions<MetadataDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenant = tenantContext;

    public DbSet<PageDefinition> Pages => Set<PageDefinition>();
    public DbSet<FormDefinition> Forms => Set<FormDefinition>();
    public DbSet<GridDefinition> Grids => Set<GridDefinition>();
    public DbSet<AuthorizationGridDefinition> AuthorizationGrids => Set<AuthorizationGridDefinition>();
    public DbSet<CustomFormData> CustomFormData => Set<CustomFormData>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("metadata");

        modelBuilder.Entity<PageDefinition>(b =>
        {
            b.ToTable("PageDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Title).HasMaxLength(128).IsRequired();
            b.Property(x => x.Module).HasMaxLength(64).IsRequired();
            b.Property(x => x.Route).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<FormDefinition>(b =>
        {
            b.ToTable("FormDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.PageCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.FormCode).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.PageCode, x.FormCode }).IsUnique();

            b.HasMany(x => x.Fields)
                .WithOne()
                .HasForeignKey("FormDefinitionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FormFieldDefinition>(b =>
        {
            b.ToTable("FormFieldDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.FieldName).HasMaxLength(64).IsRequired();
            b.Property(x => x.Label).HasMaxLength(128).IsRequired();
            b.Property(x => x.DataType).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<GridDefinition>(b =>
        {
            b.ToTable("GridDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.PageCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.GridCode).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.PageCode, x.GridCode }).IsUnique();

            b.HasMany(x => x.Columns)
                .WithOne()
                .HasForeignKey("GridDefinitionId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GridColumnDefinition>(b =>
        {
            b.ToTable("GridColumnDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.FieldName).HasMaxLength(64).IsRequired();
            b.Property(x => x.HeaderText).HasMaxLength(128).IsRequired();
            b.Property(x => x.DataType).HasMaxLength(32).IsRequired();
        });

        modelBuilder.Entity<AuthorizationGridDefinition>(b =>
        {
            b.ToTable("AuthorizationGridDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.GridCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.RoleOrPermissionCode).HasMaxLength(64).IsRequired();
            b.HasIndex(x => new { x.GridCode, x.RoleOrPermissionCode }).IsUnique();
        });

        modelBuilder.Entity<CustomFormData>(b =>
        {
            b.ToTable("CustomFormData");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
            b.Property(x => x.FormCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.DataJson).HasColumnType("jsonb");

            b.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.FormCode }).IsUnique();

            // Tenant global query filter
            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });
    }
}
