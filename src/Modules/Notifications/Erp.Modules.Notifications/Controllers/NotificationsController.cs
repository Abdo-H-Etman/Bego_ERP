using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Modules.Notifications.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken ct)
    {
        await service.SendAsync(request.Title, request.Message, request.Type, request.RecipientUserIds, ct);
        return Accepted();
    }

    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserNotifications(Guid userId, [FromQuery] bool? unreadOnly, CancellationToken ct)
    {
        var list = await service.GetUserNotificationsAsync(userId, unreadOnly ?? false, ct);
        return Ok(list);
    }

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAsRead(Guid id, [FromQuery] Guid userId, CancellationToken ct)
    {
        await service.MarkAsReadAsync(id, userId, ct);
        return NoContent();
    }
}

public record SendNotificationRequest(string Title, string Message, NotificationType Type, List<Guid> RecipientUserIds);
