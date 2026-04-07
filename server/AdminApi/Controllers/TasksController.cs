using AdminApi.Models.DTOs;
using AdminApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdminApi.Controllers;

[ApiController]
[Route("api/admin/tasks")]
[Produces("application/json")]
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
    /// Получить все задачи
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        _logger.LogInformation("Getting all tasks");
        var tasks = await _taskService.GetAllTasksAsync();
        return Ok(tasks);
    }

    /// <summary>
    /// Получить задачу по ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        _logger.LogInformation("Getting task with id: {TaskId}", id);
        var task = await _taskService.GetTaskByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task with id {TaskId} not found", id);
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
        _logger.LogInformation("Creating new task with title: {Title}", dto.Title);
        var task = await _taskService.CreateTaskAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
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
        _logger.LogInformation("Updating task with id: {TaskId}", id);
        var task = await _taskService.UpdateTaskAsync(id, dto);

        if (task is null)
        {
            _logger.LogWarning("Task with id {TaskId} not found", id);
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
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Deleting task with id: {TaskId}", id);
        var result = await _taskService.DeleteTaskAsync(id);

        if (!result)
        {
            _logger.LogWarning("Task with id {TaskId} not found", id);
            return NotFound(new { message = $"Task with id '{id}' not found" });
        }

        return NoContent();
    }
}