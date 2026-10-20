using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tenancy.Application;
using Tenancy.Domain;

namespace Tenancy.Infrastructure;

public class TenancyDbContext(DbContextOptions<TenancyDbContext> options) : DbContext(options), ITenancyDbContext
{
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction ??= await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.CommitAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("tenancy");

        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("Tenants", t =>
            {
                t.HasCheckConstraint("CK_Tenant_Id", "\"AppId\" IS NOT NULL OR \"LegalId\" IS NOT NULL");
            });

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();

            b.Property(x => x.AppId).HasMaxLength(64);
            b.Property(x => x.LegalId).HasMaxLength(64);
            b.Property(x => x.CountryCode).HasMaxLength(2).IsFixedLength();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            b.Property(x => x.ShardKey).HasMaxLength(32);
            b.Property(x => x.CustomConnectionString).HasMaxLength(512);

            b.HasIndex(x => x.AppId)
                .IsUnique()
                .HasDatabaseName("UQ_Tenant_App")
                .HasFilter("\"AppId\" IS NOT NULL");

            b.HasIndex(x => new { x.CountryCode, x.LegalId })
                .IsUnique()
                .HasDatabaseName("UQ_Tenant_Legal")
                .HasFilter("\"LegalId\" IS NOT NULL");
        });
    }
}
