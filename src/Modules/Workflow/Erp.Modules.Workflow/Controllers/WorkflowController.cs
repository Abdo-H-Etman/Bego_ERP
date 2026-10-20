using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Erp.Modules.Workflow.Controllers;

[ApiController]
[Route("api/workflow")]
public class WorkflowController(IWorkflowEngine engine) : ControllerBase
{
    [HttpPost("instances/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request, CancellationToken ct)
    {
        var result = await engine.StartWorkflowAsync(request.WorkflowCode, request.EntityType, request.EntityId, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("instances/{id:guid}/actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerAction(Guid id, [FromBody] TriggerActionRequest request, CancellationToken ct)
    {
        var result = await engine.TriggerActionAsync(id, request.ActionName, request.PerformedBy, request.UserRoles ?? [], request.Comments, ct);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("instances/{id:guid}/available-actions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableActions(Guid id, [FromQuery] string[] roles, CancellationToken ct)
    {
        var actions = await engine.GetAvailableActionsAsync(id, roles, ct);
        return Ok(actions);
    }
}

public record StartWorkflowRequest(string WorkflowCode, string EntityType, Guid EntityId);
public record TriggerActionRequest(string ActionName, string PerformedBy, string[]? UserRoles, string? Comments);
