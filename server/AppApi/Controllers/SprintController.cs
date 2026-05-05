using AppApi.Services.Interfaces;
using AppApi.Models.DTOs;
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
    private readonly ILogger<SprintController> _logger;
    private readonly ISprintService _sprintService;
    private readonly IFlowPhaseService _flowPhaseService;

    public SprintController(
        ISprintService sprintService,
        ITaskService taskService,
        IFlowPhaseService flowPhaseService,
        ILogger<SprintController> logger)
    {
        _sprintService = sprintService;
        _taskService = taskService;
        _flowPhaseService = flowPhaseService;
        _logger = logger;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    /// <summary>
    /// Получить все задачи текущего спринта
    /// </summary>
    [HttpGet("tasks")]
    public async Task<IActionResult> GetSprintTasks()
        => Ok(await _taskService.GetSprintTasksAsync(GetCurrentUserId()));

    /// <summary>
    /// Получить текущее состояние спринта и фазу пользователя
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SprintStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Fetching sprint status for user {UserId}", userId);

        var status = await _sprintService.GetStatusAsync(userId);
        return Ok(status);
    }

    /// <summary>
    /// Запустить новый спринт с выбранными задачами
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartSprintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartSprint([FromBody] StartSprintRequestDto dto)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} attempting to start a sprint", userId);

        try
        {
            var result = await _sprintService.StartSprintAsync(dto.TaskIds, userId);

            // Обновляем flow-фазу только после успешного старта спринта
            await _flowPhaseService.SetPhaseAsync(userId, "sprint");

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Sprint start conflict for user {UserId}: {Message}", userId, ex.Message);
            return Conflict(new { message = ex.Message }); // 409
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message }); // 400
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid(); // 403
        }
    }

    /// <summary>
    /// Завершить текущий активный спринт
    /// </summary>
    [HttpPost("complete")]
    [ProducesResponseType(typeof(CompleteSprintResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSprint()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("User {UserId} attempting to complete sprint", userId);

        try
        {
            var result = await _sprintService.CompleteSprintAsync(userId);

            // Обновляем flow-фазу только после успешного завершения спринта
            await _flowPhaseService.SetPhaseAsync(userId, "review");

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Complete sprint failed for user {UserId}: {Message}", userId, ex.Message);
            return NotFound(new { message = ex.Message }); // 404
        }
    }
}