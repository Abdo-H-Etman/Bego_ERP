using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Api.Controllers;

[ApiController]
[Route("/")]
public class StatusController : ControllerBase
{
    private static readonly string[] LoadedModules =
    [
        "Tenancy",
        "Accounting",
        "Inventory",
        "Sales",
        "Purchasing",
        "Identity",
        "Metadata",
        "Workflow",
        "Notifications"
    ];

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            Name = "ERP Monolith",
            Status = "Healthy",
            LoadedModules
        });
    }
}
