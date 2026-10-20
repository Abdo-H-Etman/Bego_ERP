using Erp.SharedKernel;

namespace Erp.Modules.Metadata.Domain;

public class PageDefinition : Entity<Guid>, IAuditableEntity
{
    private PageDefinition() { }

    public PageDefinition(
        Guid id,
        string code,
        string title,
        string module,
        string route,
        string[] requiredRoles,
        string[] requiredPermissions)
        : base(id)
    {
        Code = code.Trim().ToLowerInvariant();
        Title = title.Trim();
        Module = module.Trim();
        Route = route.Trim();
        RequiredRoles = requiredRoles;
        RequiredPermissions = requiredPermissions;
    }

    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Module { get; private set; } = string.Empty;
    public string Route { get; private set; } = string.Empty;
    public string[] RequiredRoles { get; private set; } = [];
    public string[] RequiredPermissions { get; private set; } = [];

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
}

public class FormDefinition : Entity<Guid>
{
    private readonly List<FormFieldDefinition> _fields = [];

    private FormDefinition() { }

    public FormDefinition(Guid id, string pageCode, string formCode, string title)
        : base(id)
    {
        PageCode = pageCode.Trim().ToLowerInvariant();
        FormCode = formCode.Trim().ToLowerInvariant();
        Title = title.Trim();
    }

    public string PageCode { get; private set; } = string.Empty;
    public string FormCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public IReadOnlyCollection<FormFieldDefinition> Fields => _fields.AsReadOnly();

    public void AddField(FormFieldDefinition field) => _fields.Add(field);
}

public class FormFieldDefinition(
    Guid id,
    string fieldName,
    string label,
    string dataType,
    bool isRequired,
    string? defaultValue = null,
    string? optionsJson = null,
    int order = 0) : Entity<Guid>(id)
{
    public string FieldName { get; set; } = fieldName;
    public string Label { get; set; } = label;
    public string DataType { get; set; } = dataType; // text, number, date, select, checkbox, textarea
    public bool IsRequired { get; set; } = isRequired;
    public string? DefaultValue { get; set; } = defaultValue;
    public string? OptionsJson { get; set; } = optionsJson;
    public int Order { get; set; } = order;
}

public class GridDefinition : Entity<Guid>
{
    private readonly List<GridColumnDefinition> _columns = [];

    private GridDefinition() { }

    public GridDefinition(Guid id, string pageCode, string gridCode, string title, int pageSize = 20)
        : base(id)
    {
        PageCode = pageCode.Trim().ToLowerInvariant();
        GridCode = gridCode.Trim().ToLowerInvariant();
        Title = title.Trim();
        PageSize = pageSize;
    }

    public string PageCode { get; private set; } = string.Empty;
    public string GridCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int PageSize { get; private set; } = 20;
    public IReadOnlyCollection<GridColumnDefinition> Columns => _columns.AsReadOnly();

    public void AddColumn(GridColumnDefinition column) => _columns.Add(column);
}

public class GridColumnDefinition(
    Guid id,
    string fieldName,
    string headerText,
    string dataType,
    bool isSortable,
    bool isFilterable,
    int width = 150,
    int order = 0) : Entity<Guid>(id)
{
    public string FieldName { get; set; } = fieldName;
    public string HeaderText { get; set; } = headerText;
    public string DataType { get; set; } = dataType;
    public bool IsSortable { get; set; } = isSortable;
    public bool IsFilterable { get; set; } = isFilterable;
    public int Width { get; set; } = width;
    public int Order { get; set; } = order;
}

public class AuthorizationGridDefinition : Entity<Guid>
{
    private AuthorizationGridDefinition() { }

    public AuthorizationGridDefinition(
        Guid id,
        string gridCode,
        string roleOrPermissionCode,
        string[] allowedColumns,
        string[] hiddenColumns,
        string[] readOnlyColumns,
        bool allowExport = true,
        bool allowInlineEdit = false)
        : base(id)
    {
        GridCode = gridCode.Trim().ToLowerInvariant();
        RoleOrPermissionCode = roleOrPermissionCode.Trim();
        AllowedColumns = allowedColumns;
        HiddenColumns = hiddenColumns;
        ReadOnlyColumns = readOnlyColumns;
        AllowExport = allowExport;
        AllowInlineEdit = allowInlineEdit;
    }

    public string GridCode { get; private set; } = string.Empty;
    public string RoleOrPermissionCode { get; private set; } = string.Empty;
    public string[] AllowedColumns { get; private set; } = [];
    public string[] HiddenColumns { get; private set; } = [];
    public string[] ReadOnlyColumns { get; private set; } = [];
    public bool AllowExport { get; private set; } = true;
    public bool AllowInlineEdit { get; private set; } = false;
}

public class CustomFormData : AggregateRoot<Guid>, IAuditableEntity
{
    private CustomFormData() { }

    public CustomFormData(Guid id, Guid tenantId, string entityType, Guid entityId, string formCode, string dataJson)
        : base(id)
    {
        TenantId = tenantId;
        EntityType = entityType.Trim().ToLowerInvariant();
        EntityId = entityId;
        FormCode = formCode.Trim().ToLowerInvariant();
        DataJson = dataJson;
    }

    public Guid TenantId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string FormCode { get; private set; } = string.Empty;
    public string DataJson { get; private set; } = "{}";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public void UpdateData(string dataJson)
    {
        DataJson = dataJson;
    }
}
