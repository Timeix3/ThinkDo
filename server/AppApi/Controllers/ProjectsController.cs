using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IProjectService projectService, ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _projectService.GetProjectsAsync(GetCurrentUserId()));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _projectService.GetProjectByIdAsync(id, GetCurrentUserId());
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _projectService.CreateProjectAsync(dto, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto)
    {
        try
        {
            var result = await _projectService.UpdateProjectAsync(id, dto, GetCurrentUserId());
            return result == null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _projectService.DeleteProjectAsync(id, GetCurrentUserId());
            return result ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/tasks")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectTasks(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var tasks = await _projectService.GetProjectTasksAsync(id, userId);
            return Ok(tasks);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Failed to get tasks: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
    }
}