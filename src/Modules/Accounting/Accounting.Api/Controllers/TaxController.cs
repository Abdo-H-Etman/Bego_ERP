using Accounting.Application.Tax;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Accounting.Api.Controllers;

[ApiController]
[Route("api/accounting/[controller]")]
public class TaxController(ITaxEngineFactory factory) : ControllerBase
{
    [HttpGet("calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Calculate([FromQuery] decimal subtotal)
    {
        var engine = factory.GetEngine();
        var result = engine.Calculate(subtotal);
        return Ok(result);
    }
}
