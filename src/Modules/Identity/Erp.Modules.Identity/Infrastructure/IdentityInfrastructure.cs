using System.Collections.Concurrent;
using Erp.Application.Abstractions.Tenancy;
using Erp.Modules.Identity.Application;
using Erp.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Identity.Infrastructure;

public class IdentityDbContext(
    DbContextOptions<IdentityDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenant = tenantContext;

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");

        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("Users");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Email).HasMaxLength(128).IsRequired();
            b.Property(x => x.FullName).HasMaxLength(128).IsRequired();
            b.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();

            b.HasMany(x => x.Roles)
                .WithOne()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<Role>(b =>
        {
            b.ToTable("Roles");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).ValueGeneratedNever();
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Name).HasMaxLength(64).IsRequired();
            b.Property(x => x.Description).HasMaxLength(256);
            b.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();

            b.HasMany(x => x.Permissions)
                .WithOne()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<Permission>(b =>
        {
            b.ToTable("Permissions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Name).HasMaxLength(128).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.ToTable("UserRoles");
            b.HasKey(x => new { x.UserId, x.RoleId });
        });

        modelBuilder.Entity<RolePermission>(b =>
        {
            b.ToTable("RolePermissions");
            b.HasKey(x => new { x.RoleId, x.PermissionId });
        });
    }
}

public class PermissionService(IdentityDbContext dbContext) : IPermissionService
{
    private static readonly ConcurrentDictionary<Guid, (DateTime CacheTime, HashSet<string> Permissions)> UserPermissionCache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(10);

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    public async Task<HashSet<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (UserPermissionCache.TryGetValue(userId, out var entry) &&
            DateTime.UtcNow - entry.CacheTime < CacheExpiry)
        {
            return entry.Permissions;
        }

        var permissionCodes = await (
            from ur in dbContext.UserRoles
            where ur.UserId == userId
            join rp in dbContext.RolePermissions on ur.RoleId equals rp.RoleId
            join p in dbContext.Permissions on rp.PermissionId equals p.Id
            select p.Code
        ).Distinct().ToListAsync(cancellationToken);

        var result = new HashSet<string>(permissionCodes, StringComparer.OrdinalIgnoreCase);
        UserPermissionCache[userId] = (DateTime.UtcNow, result);
        return result;
    }

    public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions.AsNoTracking().ToListAsync(cancellationToken);
    }
}
