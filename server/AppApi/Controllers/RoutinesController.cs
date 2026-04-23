using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/routines")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class RoutinesController : ControllerBase
{
    private readonly IRoutineService _routineService;
    private readonly ILogger<RoutinesController> _logger;

    public RoutinesController(IRoutineService routineService, ILogger<RoutinesController> logger)
    {
        _routineService = routineService;
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
    /// Получить все рутины текущего пользователя
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoutineResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("Getting all routines for user {UserId}", userId);
        var routines = await _routineService.GetAllRoutinesAsync(userId);

        return Ok(routines);
    }

    /// <summary>
    /// Получить рутину по ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RoutineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} getting routine {RoutineId}", userId, id);
        var routine = await _routineService.GetRoutineByIdAsync(id, userId);

        if (routine is null)
        {
            _logger.LogWarning("Routine {RoutineId} not found or access denied for user {UserId}", id, userId);
            return NotFound(new { message = $"Routine with id '{id}' not found" });
        }

        return Ok(routine);
    }

    /// <summary>
    /// Создать новую рутину
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoutineResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRoutineDto dto)
    {
        var userId = GetCurrentUserId();

        // Валидация пустого имени
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogWarning("User {UserId} attempted to create routine with empty name", userId);
            return BadRequest(new { message = "Name cannot be empty or contain only whitespace" });
        }

        _logger.LogInformation("User {UserId} creating new routine with name: {Name}, frequency: {Frequency}",
            userId, dto.Name, dto.Frequency);

        try
        {
            var routine = await _routineService.CreateRoutineAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = routine.Id }, routine);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить рутину
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RoutineResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRoutineDto dto)
    {
        var userId = GetCurrentUserId();

        // Валидация пустого имени
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            _logger.LogWarning("User {UserId} attempted to update routine {RoutineId} with empty name", userId, id);
            return BadRequest(new { message = "Name cannot be empty or contain only whitespace" });
        }

        _logger.LogInformation("User {UserId} updating routine {RoutineId}", userId, id);

        try
        {
            var routine = await _routineService.UpdateRoutineAsync(id, dto, userId);

            if (routine is null)
            {
                _logger.LogWarning("Routine {RoutineId} not found or access denied for user {UserId}", id, userId);
                return NotFound(new { message = $"Routine with id '{id}' not found" });
            }

            return Ok(routine);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Мягкое удаление рутины
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} soft deleting routine {RoutineId}", userId, id);
        var result = await _routineService.SoftDeleteRoutineAsync(id, userId);

        if (!result)
        {
            _logger.LogWarning("Routine {RoutineId} not found or already deleted for user {UserId}", id, userId);
            return NotFound(new { message = $"Routine with id '{id}' not found" });
        }

        return NoContent();
    }
}