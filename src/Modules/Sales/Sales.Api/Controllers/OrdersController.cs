using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Sales.Application.Features.Orders.Commands;
using Sales.Application.Features.Orders.Queries;

namespace Sales.Api.Controllers;

[ApiController]
[Route("api/sales/[controller]")]
public class OrdersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return Created($"/api/sales/orders/{result.Value}", new { OrderId = result.Value });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrdersQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
