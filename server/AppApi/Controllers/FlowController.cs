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
    private readonly ILogger<FlowController> _logger;

    public FlowController(IFlowPhaseService flowPhaseService, ILogger<FlowController> logger)
    {
        _flowPhaseService = flowPhaseService;
        _logger = logger;
    }

    private string GetCurrentUserId()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User ID not found in token claims");
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet("phase")]
    [ProducesResponseType(typeof(FlowPhaseResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPhase()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting flow phase for user {UserId}", userId);

        var phase = await _flowPhaseService.GetPhaseAsync(userId);

        _logger.LogInformation("Flow phase for user {UserId} is {Phase}", userId, phase);
        return Ok(new FlowPhaseResponse(phase));
    }

    [HttpPut("phase")]
    [ProducesResponseType(typeof(FlowPhaseUpdateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePhase([FromBody] FlowPhaseUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Updating flow phase for user {UserId} to {Phase}", userId, request.Phase);

        try
        {
            var phase = await _flowPhaseService.SetPhaseAsync(userId, request.Phase);

            _logger.LogInformation("Flow phase updated successfully for user {UserId}", userId);
            return Ok(new FlowPhaseUpdateResponse(true, phase));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid phase update attempt for user {UserId}: {Message}", userId, ex.Message);
            return BadRequest(new { error = "Invalid phase" });
        }
    }

    public record FlowPhaseResponse(string Phase);
    public record FlowPhaseUpdateRequest(string Phase);
    public record FlowPhaseUpdateResponse(bool Success, string Phase);
}