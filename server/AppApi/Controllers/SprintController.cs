using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/sprint")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class SprintController : ControllerBase
{
    private readonly ITaskService _taskService;

    public SprintController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Получить все задачи текущего спринта
    /// </summary>
    [HttpGet("tasks")]
    public async Task<IActionResult> GetSprintTasks()
        => Ok(await _taskService.GetSprintTasksAsync(GetCurrentUserId()));
}