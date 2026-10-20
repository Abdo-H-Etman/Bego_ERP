using Erp.Modules.Identity.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Modules.Identity.Controllers;

[ApiController]
[Route("api/identity/[controller]")]
public class PermissionsController(IPermissionService permissionService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions(CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetAllPermissionsAsync(cancellationToken);
        return Ok(permissions);
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserPermissions(Guid id, CancellationToken cancellationToken)
    {
        var permissions = await permissionService.GetUserPermissionsAsync(id, cancellationToken);
        return Ok(permissions);
    }
}
