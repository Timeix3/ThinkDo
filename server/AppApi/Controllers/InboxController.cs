// AppApi/Controllers/InboxController.cs
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AppApi.Controllers;

[ApiController]
[Route("api/inbox")]
[Produces("application/json")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class InboxController : ControllerBase
{
    private readonly IInboxService _inboxService;
    private readonly ILogger<InboxController> _logger;

    public InboxController(IInboxService inboxService, ILogger<InboxController> logger)
    {
        _inboxService = inboxService;
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
    /// Получить все записи Inbox текущего пользователя
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(InboxListResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("Getting all inbox items for user {UserId}", userId);
        var result = await _inboxService.GetAllItemsAsync(userId);

        // Добавляем флаг переполнения в заголовок
        Response.Headers.Append("X-Inbox-Overflow", result.InboxOverflow.ToString().ToLowerInvariant());

        return Ok(result);
    }

    /// <summary>
    /// Создать новую запись в Inbox
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(InboxItemResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInboxItemDto dto)
    {
        var userId = GetCurrentUserId();

        // Валидация пустого title или только пробелов
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            _logger.LogWarning("User {UserId} attempted to create inbox item with empty title", userId);
            return BadRequest(new { message = "Title cannot be empty or contain only whitespace" });
        }

        _logger.LogInformation("User {UserId} creating new inbox item with title: {Title}", userId, dto.Title);

        try
        {
            var item = await _inboxService.CreateItemAsync(dto, userId);
            return Created($"/api/inbox/{item.Id}", item);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Обновление заголовка записи Inbox
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInboxItemDto dto)
    {
        var userId = GetCurrentUserId();

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title cannot be empty or contain only whitespace" });

        _logger.LogInformation("User {UserId} updating inbox item {ItemId}", userId, id);

        try
        {
            var result = await _inboxService.UpdateItemAsync(id, dto, userId);
            if (!result)
                return NotFound(new { message = $"Inbox item with id '{id}' not found" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    /// <summary>
    /// Восстановление удалённой записи Inbox (Undo delete)
    /// </summary>
    [HttpPatch("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} restoring inbox item {ItemId}", userId, id);
        var result = await _inboxService.RestoreItemAsync(id, userId);

        if (!result)
        {
            _logger.LogWarning("Inbox item {ItemId} not found or not deleted for user {UserId}", id, userId);
            return NotFound(new { message = $"Inbox item with id '{id}' not found or not deleted" });
        }

        return NoContent();
    }

    /// <summary>
    /// Мягкое удаление записи из Inbox
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetCurrentUserId();

        _logger.LogInformation("User {UserId} soft deleting inbox item {ItemId}", userId, id);
        var result = await _inboxService.SoftDeleteItemAsync(id, userId);

        if (!result)
        {
            _logger.LogWarning("Inbox item {ItemId} not found or already deleted for user {UserId}", id, userId);
            return NotFound(new { message = $"Inbox item with id '{id}' not found" });
        }

        return NoContent();
    }
}