using Accounting.Application;
using Accounting.Domain;
using Erp.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Accounting.Infrastructure;

public class AccountingDbContext(
    DbContextOptions<AccountingDbContext> options,
    ITenantContext tenantContext) : DbContext(options), IAccountingDbContext
{
    private readonly ITenantContext _tenant = tenantContext;
    private IDbContextTransaction? _currentTransaction;

    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();

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
        modelBuilder.HasDefaultSchema("accounting");

        modelBuilder.Entity<Invoice>(b =>
        {
            b.ToTable("Invoices");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();

            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.CustomerName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            b.Property(x => x.TaxAmount).HasPrecision(18, 4);
            b.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

            b.HasMany(x => x.Lines)
                .WithOne()
                .HasForeignKey("InvoiceId")
                .OnDelete(DeleteBehavior.Cascade);

            // Multitenancy Global Query Filter as specified in README Section 6
            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<InvoiceLine>(b =>
        {
            b.ToTable("InvoiceLines");
            b.HasKey(x => x.Id);
            b.Property(x => x.Description).HasMaxLength(256).IsRequired();
            b.Property(x => x.Quantity).HasPrecision(18, 4);
            b.Property(x => x.UnitPrice).HasPrecision(18, 4);
            b.Property(x => x.TaxRate).HasPrecision(18, 4);
            b.Property(x => x.LineTotal).HasPrecision(18, 4);
        });
    }
}
