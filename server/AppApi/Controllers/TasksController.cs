using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService taskService, ILogger<TasksController> logger)
    {
        _taskService = taskService;
        _logger = logger;
    }

    /// <summary>
    /// Получить ID текущего пользователя из токена
    /// </summary>
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

    /// <summary>
    /// Получить задачу "Обезьяна" на сегодня
    /// </summary>
    [HttpGet("monkey")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonkey()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting today's monkey task for user {UserId}", userId);

        var task = await _taskService.GetTodayTaskAsync(userId);

        if (task is null)
            return Ok(Array.Empty<TaskResponseDto>());

        return Ok(new[] { task });
    }

    /// <summary>
    /// Получить все задачи "Обезьяна" на сегодня
    /// </summary>
    [HttpGet("monkey/all")]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMonkeyAll()
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting all today's monkey tasks for user {UserId}", userId);

        var tasks = await _taskService.GetTodayTasksAsync(userId);
        return Ok(tasks);
    }

    /// <summary>
    /// Получить все задачи текущего пользователя с пагинацией
    /// </summary>
    /// <param name="offset">Смещение (по умолчанию 0)</param>
    /// <param name="limit">Лимит записей (по умолчанию 50, максимум 100)</param>
    [HttpGet]
    [ProducesResponseType(typeof(TaskListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll([FromQuery] int offset = 0, [FromQuery] int limit = 50)
    {
        var userId = GetCurrentUserId();

        // Валидация параметров пагинации
        if (offset < 0)
        {
            return BadRequest(new { message = "Offset cannot be negative" });
        }

        if (limit <= 0 || limit > 100)
        {
            return BadRequest(new { message = "Limit must be between 1 and 100" });
        }

        _logger.LogInformation("Getting tasks for user {UserId} with offset={Offset}, limit={Limit}",
            userId, offset, limit);

        var result = await _taskService.GetAllTasksAsync(userId, offset, limit);

        // Добавляем метаданные пагинации в заголовки
        Response.Headers.Append("X-Total-Count", result.TotalCount.ToString());
        Response.Headers.Append("X-Page-Number", result.PageNumber.ToString());
        Response.Headers.Append("X-Page-Size", result.PageSize.ToString());
        Response.Headers.Append("X-Has-More", result.HasMore.ToString().ToLowerInvariant());

        return Ok(result);
    }

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} getting task {TaskId}", userId, id);
        var task = await _taskService.GetTaskByIdAsync(id, userId);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found or access denied for user {UserId}", id, userId);
            return NotFound(new { message = $"Task with id '{id}' not found" });
        }

        return Ok(task);
    }

    /// <summary>
    /// Создать новую задачу
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} creating new task with title: {Title} for project: {ProjectId}", userId, dto.Title, dto.ProjectId ?? 0);
        try
        {
            var task = await _taskService.CreateTaskAsync(dto, userId);

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }
        catch (KeyNotFoundException ex)
        {
            // Краевой случай: проект чужой или не существует (404)
            _logger.LogWarning("Project validation failed for user {UserId}: {Message}", userId, ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            // Краевой случай: проект удален или другие ошибки валидации (400)
            _logger.LogWarning("Validation error for user {UserId}: {Message}", userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTaskDto dto)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} updating task {TaskId}", userId, id);
        var task = await _taskService.UpdateTaskAsync(id, dto, userId);

        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found or access denied for user {UserId}", id, userId);
            return NotFound(new { message = $"Task with id '{id}' not found" });
        }

        return Ok(task);
    }

    /// <summary>
    /// Удалить задачу (soft delete)
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} soft deleting task {TaskId}", userId, id);
        var result = await _taskService.DeleteTaskAsync(id, userId);

        if (!result)
        {
            _logger.LogWarning("Task {TaskId} not found or access denied for user {UserId}", id, userId);
            return NotFound(new { message = $"Task with id '{id}' not found" });
        }

        return NoContent();
    }

    /// <summary>
    /// Завершить задачу (статус → Completed)
    /// </summary>
    [HttpPatch("{id:int}/complete")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} completing task {TaskId}", userId, id);

        try
        {
            var task = await _taskService.CompleteTaskAsync(id, userId);

            if (task is null)
            {
                _logger.LogWarning("Task {TaskId} not found for user {UserId}", id, userId);
                return NotFound(new { message = $"Task with id '{id}' not found" });
            }

            _logger.LogInformation("Task {TaskId} completed successfully by user {UserId}", id, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot complete task {TaskId} for user {UserId}: {Message}",
                id, userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Отменить задачу (статус → Cancelled)
    /// </summary>
    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} cancelling task {TaskId}", userId, id);

        try
        {
            var task = await _taskService.CancelTaskAsync(id, userId);

            if (task is null)
            {
                _logger.LogWarning("Task {TaskId} not found for user {UserId}", id, userId);
                return NotFound(new { message = $"Task with id '{id}' not found" });
            }

            _logger.LogInformation("Task {TaskId} cancelled successfully by user {UserId}", id, userId);
            return Ok(task);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot cancel task {TaskId} for user {UserId}: {Message}",
                id, userId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }
}