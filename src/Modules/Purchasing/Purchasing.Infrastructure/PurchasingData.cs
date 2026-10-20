using Erp.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Purchasing.Application;
using Purchasing.Domain;

namespace Purchasing.Infrastructure;

public class PurchasingDbContext(
    DbContextOptions<PurchasingDbContext> options,
    ITenantContext tenantContext) : DbContext(options), IPurchasingDbContext
{
    private readonly ITenantContext _tenant = tenantContext;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();

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
        modelBuilder.HasDefaultSchema("purchasing");

        modelBuilder.Entity<PurchaseOrder>(b =>
        {
            b.ToTable("PurchaseOrders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.VendorName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

            b.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey("PurchaseOrderId")
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<PurchaseOrderLine>(b =>
        {
            b.ToTable("PurchaseOrderLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.ItemName).HasMaxLength(128).IsRequired();
            b.Property(x => x.UnitCost).HasPrecision(18, 4);
            b.Property(x => x.LineTotal).HasPrecision(18, 4);
        });
    }
}
