using Erp.SharedKernel;

namespace Erp.Modules.Identity.Domain;

public class User : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<UserRole> _roles = [];

    private User() { }

    public User(Guid id, Guid tenantId, string email, string fullName, string passwordHash)
        : base(id)
    {
        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        FullName = fullName.Trim();
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public void AssignRole(Guid roleId)
    {
        if (_roles.All(r => r.RoleId != roleId))
        {
            _roles.Add(new UserRole(Id, roleId));
        }
    }

    public void RemoveRole(Guid roleId)
    {
        _roles.RemoveAll(r => r.RoleId == roleId);
    }
}

public class Role : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<RolePermission> _permissions = [];

    private Role() { }

    public Role(Guid id, Guid tenantId, string name, string description)
        : base(id)
    {
        TenantId = tenantId;
        Name = name.Trim();
        Description = description.Trim();
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        _permissions.Clear();
        foreach (var pId in permissionIds.Distinct())
        {
            _permissions.Add(new RolePermission(Id, pId));
        }
    }
}

public class Permission : Entity<Guid>
{
    private Permission() { }

    public Permission(Guid id, string code, string name, string category, bool isPagePermission = false)
        : base(id)
    {
        Code = code.Trim();
        Name = name.Trim();
        Category = category.Trim();
        IsPagePermission = isPagePermission;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public bool IsPagePermission { get; private set; }
}

public class UserRole(Guid userId, Guid roleId)
{
    public Guid UserId { get; set; } = userId;
    public Guid RoleId { get; set; } = roleId;
}

public class RolePermission(Guid roleId, Guid permissionId)
{
    public Guid RoleId { get; set; } = roleId;
    public Guid PermissionId { get; set; } = permissionId;
}
