using Erp.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Sales.Application;
using Sales.Domain;

namespace Sales.Infrastructure;

public class SalesDbContext(
    DbContextOptions<SalesDbContext> options,
    ITenantContext tenantContext) : DbContext(options), ISalesDbContext
{
    private readonly ITenantContext _tenant = tenantContext;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

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
        modelBuilder.HasDefaultSchema("sales");

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("Orders");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.CustomerName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

            b.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<OrderLine>(b =>
        {
            b.ToTable("OrderLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.ProductName).HasMaxLength(128).IsRequired();
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.LineTotal).HasPrecision(18, 4);
        });
    }
}
