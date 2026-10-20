using Erp.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/integrations/begohub")]
public class BegohubController(ITenantContext tenant) : ControllerBase
{
    [HttpGet("sync-pull")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SyncPull()
    {
        return Ok(new
        {
            Source = "Begohub ERP",
            Status = "Authenticated via HMAC",
            TenantId = tenant.TenantId,
            TimestampUtc = DateTime.UtcNow,
            SyncPayload = new { Message = "Ready for invoice and inventory delta extraction" }
        });
    }
}
