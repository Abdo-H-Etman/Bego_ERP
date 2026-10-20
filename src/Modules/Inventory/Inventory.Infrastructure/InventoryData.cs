using Erp.Application.Abstractions.Tenancy;
using Inventory.Application;
using Inventory.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventory.Infrastructure;

public class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    ITenantContext tenantContext) : DbContext(options), IInventoryDbContext
{
    private readonly ITenantContext _tenant = tenantContext;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Product> Products => Set<Product>();

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
        modelBuilder.HasDefaultSchema("inventory");

        modelBuilder.Entity<Product>(b =>
        {
            b.ToTable("Products");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });
    }
}
