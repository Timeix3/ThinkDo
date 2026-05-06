using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/flow")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class FlowController : ControllerBase
{
    private readonly IFlowPhaseService _flowPhaseService;

    public FlowController(IFlowPhaseService flowPhaseService)
    {
        _flowPhaseService = flowPhaseService;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    [HttpGet("phase")]
    [ProducesResponseType(typeof(FlowPhaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhase()
    {
        var phase = await _flowPhaseService.GetPhaseAsync(GetCurrentUserId());
        return Ok(new FlowPhaseResponse(phase));
    }

    [HttpPut("phase")]
    [ProducesResponseType(typeof(FlowPhaseUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePhase([FromBody] FlowPhaseUpdateRequest request)
    {
        try
        {
            var phase = await _flowPhaseService.SetPhaseAsync(GetCurrentUserId(), request.Phase);
            return Ok(new FlowPhaseUpdateResponse(true, phase));
        }
        catch (ArgumentException)
        {
            return BadRequest(new { error = "Invalid phase" });
        }
    }

    public record FlowPhaseResponse(string Phase);
    public record FlowPhaseUpdateRequest(string Phase);
    public record FlowPhaseUpdateResponse(bool Success, string Phase);
}
