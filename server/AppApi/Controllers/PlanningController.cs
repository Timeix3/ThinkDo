using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/planning")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class PlanningController : ControllerBase
{
    private readonly IProjectService _projectService;

    public PlanningController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    
    /// <summary>
    /// Проекты с их задачами для отображения в Планировании
    /// </summary>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(IEnumerable<PlanningProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjects()
        => Ok(await _projectService.GetPlanningProjectsAsync(GetCurrentUserId()));
}