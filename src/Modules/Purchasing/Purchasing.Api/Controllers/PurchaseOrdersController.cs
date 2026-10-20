using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Purchasing.Application.Features.PurchaseOrders.Commands;
using Purchasing.Application.Features.PurchaseOrders.Queries;

namespace Purchasing.Api.Controllers;

[ApiController]
[Route("api/purchasing/purchase-orders")]
public class PurchaseOrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Created($"/api/purchasing/purchase-orders/{result.Value}", new { PurchaseOrderId = result.Value });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrders(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPurchaseOrdersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
