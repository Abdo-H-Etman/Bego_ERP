using Accounting.Application.Features.Invoices.Commands;
using Accounting.Application.Features.Invoices.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/accounting/[controller]")]
public class InvoicesController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInvoiceCommand(
            request.CustomerName,
            request.Currency ?? "EGP",
            request.Lines.Select(l => new InvoiceLineDto(l.Description, l.Quantity, l.UnitPrice)).ToList());

        var result = await sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return CreatedAtAction(nameof(GetInvoiceById), new { id = result.Value }, new { InvoiceId = result.Value });
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvoiceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoicesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(InvoiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetInvoiceByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}

public record CreateInvoiceRequest(
    string CustomerName,
    string? Currency,
    List<InvoiceLineRequest> Lines);

public record InvoiceLineRequest(
    string Description,
    decimal Quantity,
    decimal UnitPrice);
