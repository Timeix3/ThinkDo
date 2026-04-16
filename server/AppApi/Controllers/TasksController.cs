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
    /// Получить все задачи текущего пользователя
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("Getting all tasks for user {UserId}", userId);
        var tasks = await _taskService.GetAllTasksAsync(userId);

        return Ok(tasks);
    }

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    public async Task<IActionResult> Create([FromBody] CreateTaskDto dto)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} creating new task with title: {Title}", userId, dto.Title);
        var task = await _taskService.CreateTaskAsync(dto, userId);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>
    /// Обновить задачу
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// Удалить задачу
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} deleting task {TaskId}", userId, id);
        var result = await _taskService.DeleteTaskAsync(id, userId);

        if (!result)
        {
            _logger.LogWarning("Task {TaskId} not found or access denied for user {UserId}", id, userId);
            return NotFound(new { message = $"Task with id '{id}' not found" });
        }

        return NoContent();
    }
}
