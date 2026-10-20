using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Modules;
using Erp.Infrastructure.Abstractions.Tenancy;
using Erp.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Workflow;

// --- Domain ---
public class WorkflowDefinition : Entity<Guid>
{
    private readonly List<WorkflowState> _states = [];
    private readonly List<WorkflowTransition> _transitions = [];

    private WorkflowDefinition() { }

    public WorkflowDefinition(Guid id, string code, string name, string entityType, string initialState)
        : base(id)
    {
        Code = code.Trim().ToLowerInvariant();
        Name = name.Trim();
        EntityType = entityType.Trim().ToLowerInvariant();
        InitialState = initialState.Trim().ToUpperInvariant();
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string InitialState { get; private set; } = string.Empty;

    public IReadOnlyCollection<WorkflowState> States => _states.AsReadOnly();
    public IReadOnlyCollection<WorkflowTransition> Transitions => _transitions.AsReadOnly();

    public void AddState(string stateCode, string stateName, bool isFinal = false) =>
        _states.Add(new WorkflowState(Guid.NewGuid(), Id, stateCode.ToUpperInvariant(), stateName, isFinal));

    public void AddTransition(string fromState, string toState, string actionName, string[] allowedRoles) =>
        _transitions.Add(new WorkflowTransition(Guid.NewGuid(), Id, fromState.ToUpperInvariant(), toState.ToUpperInvariant(), actionName, allowedRoles));
}

public class WorkflowState(Guid id, Guid workflowId, string stateCode, string stateName, bool isFinal)
    : Entity<Guid>(id)
{
    public Guid WorkflowId { get; set; } = workflowId;
    public string StateCode { get; set; } = stateCode;
    public string StateName { get; set; } = stateName;
    public bool IsFinal { get; set; } = isFinal;
}

public class WorkflowTransition(Guid id, Guid workflowId, string fromState, string toState, string actionName, string[] allowedRoles)
    : Entity<Guid>(id)
{
    public Guid WorkflowId { get; set; } = workflowId;
    public string FromState { get; set; } = fromState;
    public string ToState { get; set; } = toState;
    public string ActionName { get; set; } = actionName;
    public string[] AllowedRoles { get; set; } = allowedRoles;
}

public class WorkflowInstance : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<WorkflowHistoryEntry> _history = [];

    private WorkflowInstance() { }

    public WorkflowInstance(Guid id, Guid tenantId, string workflowCode, string entityType, Guid entityId, string currentState)
        : base(id)
    {
        TenantId = tenantId;
        WorkflowCode = workflowCode.Trim().ToLowerInvariant();
        EntityType = entityType.Trim().ToLowerInvariant();
        EntityId = entityId;
        CurrentState = currentState.Trim().ToUpperInvariant();
    }

    public Guid TenantId { get; private set; }
    public string WorkflowCode { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public string CurrentState { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public IReadOnlyCollection<WorkflowHistoryEntry> History => _history.AsReadOnly();

    public void TransitionTo(string newState, string actionName, string performedBy, string? comments = null)
    {
        var entry = new WorkflowHistoryEntry(Guid.NewGuid(), Id, CurrentState, newState, actionName, performedBy, comments, DateTime.UtcNow);
        _history.Add(entry);
        CurrentState = newState;
    }
}

public class WorkflowHistoryEntry(
    Guid id,
    Guid instanceId,
    string fromState,
    string toState,
    string actionName,
    string performedBy,
    string? comments,
    DateTime timestampUtc) : Entity<Guid>(id)
{
    public Guid InstanceId { get; set; } = instanceId;
    public string FromState { get; set; } = fromState;
    public string ToState { get; set; } = toState;
    public string ActionName { get; set; } = actionName;
    public string PerformedBy { get; set; } = performedBy;
    public string? Comments { get; set; } = comments;
    public DateTime TimestampUtc { get; set; } = timestampUtc;
}

// --- Application Service ---
public interface IWorkflowEngine
{
    Task<Result<WorkflowInstance>> StartWorkflowAsync(string workflowCode, string entityType, Guid entityId, CancellationToken ct = default);
    Task<Result<WorkflowInstance>> TriggerActionAsync(Guid instanceId, string actionName, string performedBy, string[] userRoles, string? comments = null, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAvailableActionsAsync(Guid instanceId, string[] userRoles, CancellationToken ct = default);
}

public class WorkflowEngine(WorkflowDbContext dbContext, ITenantContext tenantContext) : IWorkflowEngine
{
    public async Task<Result<WorkflowInstance>> StartWorkflowAsync(string workflowCode, string entityType, Guid entityId, CancellationToken ct = default)
    {
        var def = await dbContext.Definitions
            .Include(d => d.States)
            .FirstOrDefaultAsync(d => d.Code == workflowCode.ToLowerInvariant(), ct);

        if (def is null) return Result.Failure<WorkflowInstance>(new Error("Workflow.NotFound", $"Workflow definition '{workflowCode}' was not found."));

        var instance = new WorkflowInstance(Guid.NewGuid(), tenantContext.TenantId, def.Code, entityType, entityId, def.InitialState);
        await dbContext.Instances.AddAsync(instance, ct);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(instance);
    }

    public async Task<Result<WorkflowInstance>> TriggerActionAsync(Guid instanceId, string actionName, string performedBy, string[] userRoles, string? comments = null, CancellationToken ct = default)
    {
        var instance = await dbContext.Instances.Include(i => i.History).FirstOrDefaultAsync(i => i.Id == instanceId, ct);
        if (instance is null) return Result.Failure<WorkflowInstance>(new Error("WorkflowInstance.NotFound", "Workflow instance not found."));

        var def = await dbContext.Definitions
            .Include(d => d.Transitions)
            .FirstOrDefaultAsync(d => d.Code == instance.WorkflowCode, ct);

        if (def is null) return Result.Failure<WorkflowInstance>(new Error("Workflow.NotFound", "Workflow definition not found."));

        var transition = def.Transitions.FirstOrDefault(t =>
            t.FromState.Equals(instance.CurrentState, StringComparison.OrdinalIgnoreCase) &&
            t.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase));

        if (transition is null)
            return Result.Failure<WorkflowInstance>(new Error("Workflow.InvalidAction", $"Action '{actionName}' is not allowed from current state '{instance.CurrentState}'."));

        // Role check
        if (transition.AllowedRoles.Length > 0 && !transition.AllowedRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase)))
        {
            return Result.Failure<WorkflowInstance>(new Error("Workflow.Unauthorized", $"User does not have required role for action '{actionName}'."));
        }

        instance.TransitionTo(transition.ToState, actionName, performedBy, comments);
        await dbContext.SaveChangesAsync(ct);

        return Result.Success(instance);
    }

    public async Task<IReadOnlyList<string>> GetAvailableActionsAsync(Guid instanceId, string[] userRoles, CancellationToken ct = default)
    {
        var instance = await dbContext.Instances.AsNoTracking().FirstOrDefaultAsync(i => i.Id == instanceId, ct);
        if (instance is null) return [];

        var def = await dbContext.Definitions.Include(d => d.Transitions).AsNoTracking().FirstOrDefaultAsync(d => d.Code == instance.WorkflowCode, ct);
        if (def is null) return [];

        return def.Transitions
            .Where(t => t.FromState.Equals(instance.CurrentState, StringComparison.OrdinalIgnoreCase) &&
                        (t.AllowedRoles.Length == 0 || t.AllowedRoles.Any(r => userRoles.Contains(r, StringComparer.OrdinalIgnoreCase))))
            .Select(t => t.ActionName)
            .ToList();
    }
}

// --- Infrastructure ---
public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenant = tenantContext;

    public DbSet<WorkflowDefinition> Definitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowState> States => Set<WorkflowState>();
    public DbSet<WorkflowTransition> Transitions => Set<WorkflowTransition>();
    public DbSet<WorkflowInstance> Instances => Set<WorkflowInstance>();
    public DbSet<WorkflowHistoryEntry> History => Set<WorkflowHistoryEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workflow");

        modelBuilder.Entity<WorkflowDefinition>(b =>
        {
            b.ToTable("WorkflowDefinitions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.Code).IsUnique();

            b.HasMany(x => x.States).WithOne().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Transitions).WithOne().HasForeignKey(x => x.WorkflowId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkflowInstance>(b =>
        {
            b.ToTable("WorkflowInstances");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.WorkflowCode).HasMaxLength(64).IsRequired();
            b.Property(x => x.CurrentState).HasMaxLength(32).IsRequired();

            b.HasMany(x => x.History).WithOne().HasForeignKey(x => x.InstanceId).OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });
    }
}

// --- Module ---
public class WorkflowModule : IModule
{
    public string Name => "Workflow";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkflowDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Workflow", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IWorkflowEngine, WorkflowEngine>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
