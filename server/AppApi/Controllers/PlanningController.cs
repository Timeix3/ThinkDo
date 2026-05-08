using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Common.Enums;

namespace AppApi.Controllers;

[ApiController]
[Route("api/planning")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class PlanningController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<PlanningController> _logger;

    public PlanningController(IProjectService projectService, ILogger<PlanningController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Проекты с их задачами для отображения в Планировании
    /// </summary>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(PlanningResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProjects()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting planning projects for user {UserId}", userId);

        var result = await _projectService.GetPlanningProjectsAsync(userId);
        return Ok(result);
    }
}