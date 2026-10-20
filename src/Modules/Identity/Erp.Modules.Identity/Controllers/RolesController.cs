using Erp.Application.Abstractions.Tenancy;
using Erp.Modules.Identity.Domain;
using Erp.Modules.Identity.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Identity.Controllers;

[ApiController]
[Route("api/identity/[controller]")]
public class RolesController(IdentityDbContext db, ITenantContext tenantContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role(Guid.NewGuid(), tenantContext.TenantId, request.Name, request.Description);
        if (request.PermissionIds?.Count > 0)
        {
            role.SetPermissions(request.PermissionIds);
        }

        await db.Roles.AddAsync(role, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/api/identity/roles/{role.Id}", new { RoleId = role.Id });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await db.Roles.Include(r => r.Permissions).AsNoTracking().ToListAsync(cancellationToken);
        return Ok(roles);
    }
}

public record CreateRoleRequest(string Name, string Description, List<Guid>? PermissionIds);
