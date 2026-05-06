using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/flow")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class FlowController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, string> UserPhases = new();
    private static readonly HashSet<string> AllowedPhases = new(StringComparer.OrdinalIgnoreCase)
    {
        "sprint",
        "review",
        "planning"
    };

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    [HttpGet("phase")]
    [ProducesResponseType(typeof(FlowPhaseResponse), StatusCodes.Status200OK)]
    public IActionResult GetPhase()
    {
        var userId = GetCurrentUserId();
        var phase = UserPhases.GetValueOrDefault(userId) ?? "planning";

        return Ok(new FlowPhaseResponse(phase));
    }

    [HttpPut("phase")]
    [ProducesResponseType(typeof(FlowPhaseUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult UpdatePhase([FromBody] FlowPhaseUpdateRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Phase) || !AllowedPhases.Contains(request.Phase))
        {
            return BadRequest(new { error = "Invalid phase" });
        }

        var normalizedPhase = request.Phase.ToLowerInvariant();
        var userId = GetCurrentUserId();
        UserPhases[userId] = normalizedPhase;

        return Ok(new FlowPhaseUpdateResponse(true, normalizedPhase));
    }

    public record FlowPhaseResponse(string Phase);
    public record FlowPhaseUpdateRequest(string Phase);
    public record FlowPhaseUpdateResponse(bool Success, string Phase);
}
