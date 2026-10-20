using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Modules;
using Erp.Infrastructure.Abstractions.Tenancy;
using Erp.SharedKernel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Modules.Notifications;

public enum NotificationType
{
    InApp,
    Email,
    Sms,
    Webhook
}

public class Notification : AggregateRoot<Guid>, IAuditableEntity
{
    private readonly List<NotificationRecipient> _recipients = [];

    private Notification() { }

    public Notification(Guid id, Guid tenantId, string title, string message, NotificationType type)
        : base(id)
    {
        TenantId = tenantId;
        Title = title.Trim();
        Message = message.Trim();
        Type = type;
    }

    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }

    public IReadOnlyCollection<NotificationRecipient> Recipients => _recipients.AsReadOnly();

    public void AddRecipient(Guid userId) => _recipients.Add(new NotificationRecipient(Id, userId));
}

public class NotificationRecipient(Guid notificationId, Guid userId)
{
    public Guid NotificationId { get; set; } = notificationId;
    public Guid UserId { get; set; } = userId;
    public bool IsRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
    }
}

public interface INotificationService
{
    Task SendAsync(string title, string message, NotificationType type, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken ct = default);
}

public record NotificationDto(Guid Id, string Title, string Message, string Type, bool IsRead, DateTime CreatedAtUtc);

public class NotificationService(NotificationsDbContext dbContext, ITenantContext tenantContext) : INotificationService
{
    public async Task SendAsync(string title, string message, NotificationType type, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default)
    {
        var notification = new Notification(Guid.NewGuid(), tenantContext.TenantId, title, message, type);
        foreach (var userId in recipientUserIds.Distinct())
        {
            notification.AddRecipient(userId);
        }

        await dbContext.Notifications.AddAsync(notification, ct);
        await dbContext.SaveChangesAsync(ct);
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken ct = default)
    {
        var recipient = await dbContext.Recipients
            .FirstOrDefaultAsync(r => r.NotificationId == notificationId && r.UserId == userId, ct);

        if (recipient is not null)
        {
            recipient.MarkAsRead();
            await dbContext.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool unreadOnly = false, CancellationToken ct = default)
    {
        var query = from n in dbContext.Notifications
                    join r in dbContext.Recipients on n.Id equals r.NotificationId
                    where r.UserId == userId && (!unreadOnly || !r.IsRead)
                    orderby n.CreatedAtUtc descending
                    select new NotificationDto(n.Id, n.Title, n.Message, n.Type.ToString(), r.IsRead, n.CreatedAtUtc);

        return await query.ToListAsync(ct);
    }
}

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ITenantContext tenantContext) : DbContext(options)
{
    private readonly ITenantContext _tenant = tenantContext;

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationRecipient> Recipients => Set<NotificationRecipient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<Notification>(b =>
        {
            b.ToTable("Notifications");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).IsRequired();
            b.Property(x => x.Title).HasMaxLength(128).IsRequired();
            b.Property(x => x.Message).HasMaxLength(1024).IsRequired();
            b.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);

            b.HasMany(x => x.Recipients)
                .WithOne()
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasQueryFilter(x => _tenant.TenantId == Guid.Empty || x.TenantId == _tenant.TenantId);
        });

        modelBuilder.Entity<NotificationRecipient>(b =>
        {
            b.ToTable("NotificationRecipients");
            b.HasKey(x => new { x.NotificationId, x.UserId });
        });
    }
}

public class NotificationsModule : IModule
{
    public string Name => "Notifications";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationsDbContext>((sp, options) =>
        {
            var tenant = sp.GetRequiredService<ITenantContext>();
            var resolver = sp.GetRequiredService<ITenantConnectionResolver>();
            var connectionString = resolver.Resolve("Notifications", tenant.Current);
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<INotificationService, NotificationService>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }
}
