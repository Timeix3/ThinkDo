// AppApi/Models/DTOs/InboxDtos.cs
using System.ComponentModel.DataAnnotations;

namespace AppApi.Models.DTOs;

/// <summary>
/// DTO для создания записи в Inbox
/// </summary>
public class CreateInboxItemDto
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
    public string Title { get; set; } = string.Empty;
}

/// <summary>
/// DTO для ответа с записью из Inbox
/// </summary>
public class InboxItemResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO для списка записей Inbox с флагом переполнения
/// </summary>
public class InboxListResponseDto
{
    public IEnumerable<InboxItemResponseDto> Items { get; set; } = Array.Empty<InboxItemResponseDto>();
    public bool InboxOverflow { get; set; }
}