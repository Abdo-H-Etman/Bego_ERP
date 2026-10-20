using Erp.Application.Abstractions.Tenancy;
using Erp.Infrastructure.Abstractions.Tenancy;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tenancy.Application.Features.Tenants.Commands;
using Tenancy.Application.Features.Tenants.Queries;

namespace Tenancy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterTenant([FromBody] RegisterTenantCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetTenantById), new { id = result.Value }, new { TenantId = result.Value });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TenantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTenants(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllTenantsQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-app/{appId}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantByAppId(string appId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantByAppIdQuery(appId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("by-legal/{countryCode}/{legalId}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenantByLegalId(string countryCode, string legalId, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTenantByLegalIdQuery(legalId, countryCode), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpGet("resolve")]
    [ProducesResponseType(typeof(TenantContext), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Resolve([FromQuery] string? appId, [FromQuery] string? legalId, [FromQuery] string? country, [FromServices] ITenantCatalog catalog)
    {
        TenantContext? tenant = null;
        if (!string.IsNullOrWhiteSpace(appId))
        {
            tenant = await catalog.ResolveByAppIdAsync(appId);
        }
        else if (!string.IsNullOrWhiteSpace(legalId) && !string.IsNullOrWhiteSpace(country))
        {
            tenant = await catalog.ResolveByLegalIdAsync(legalId, country);
        }

        return tenant is not null ? Ok(tenant) : NotFound(new { Message = "Tenant could not be resolved" });
    }
}
