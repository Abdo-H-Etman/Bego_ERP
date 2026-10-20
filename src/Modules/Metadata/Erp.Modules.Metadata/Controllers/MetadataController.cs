using Erp.Application.Abstractions.Tenancy;
using Erp.Modules.Metadata.Application;
using Erp.Modules.Metadata.Domain;
using Erp.Modules.Metadata.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Modules.Metadata.Controllers;

[ApiController]
[Route("api/metadata")]
public class MetadataController(
    MetadataDbContext db,
    IMetadataService metadataService,
    ITenantContext tenantContext) : ControllerBase
{
    [HttpGet("pages/{code}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPageMetadata(string code, [FromQuery] string? role)
    {
        var meta = await metadataService.GetPageMetadataAsync(code, role);
        return meta is not null
            ? Ok(meta)
            : NotFound(new { Message = $"Page metadata '{code}' not found" });
    }

    [HttpPost("pages")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePageDefinition(
        [FromBody] CreatePageDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var page = new PageDefinition(
            Guid.NewGuid(),
            request.Code,
            request.Title,
            request.Module,
            request.Route,
            request.RequiredRoles ?? [],
            request.RequiredPermissions ?? []);

        await db.Pages.AddAsync(page, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await metadataService.InvalidatePageCacheAsync(page.Code);

        return Created($"/api/metadata/pages/{page.Code}", page);
    }

    [HttpPost("data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveFormData(
        [FromBody] SaveFormDataRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await db.CustomFormData.FirstOrDefaultAsync(d =>
            d.EntityType == request.EntityType &&
            d.EntityId == request.EntityId &&
            d.FormCode == request.FormCode, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateData(request.DataJson);
        }
        else
        {
            var customData = new CustomFormData(
                Guid.NewGuid(),
                tenantContext.TenantId,
                request.EntityType,
                request.EntityId,
                request.FormCode,
                request.DataJson);

            await db.CustomFormData.AddAsync(customData, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { Status = "Success" });
    }

    [HttpGet("data/{entityType}/{entityId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFormData(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var data = await db.CustomFormData
            .Where(d => d.EntityType == entityType && d.EntityId == entityId)
            .ToListAsync(cancellationToken);
        return Ok(data);
    }
}

public record CreatePageDefinitionRequest(
    string Code,
    string Title,
    string Module,
    string Route,
    string[]? RequiredRoles,
    string[]? RequiredPermissions);

public record SaveFormDataRequest(
    string EntityType,
    Guid EntityId,
    string FormCode,
    string DataJson);
