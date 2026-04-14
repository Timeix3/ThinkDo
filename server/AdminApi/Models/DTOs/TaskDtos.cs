using System.ComponentModel.DataAnnotations;

namespace AdminApi.Models.DTOs;

/// <summary>
/// DTO для создания задачи
/// </summary>
public class CreateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Content must not exceed 2000 characters")]
    public string? Content { get; set; }
}

/// <summary>
/// DTO для обновления задачи
/// </summary>
public class UpdateTaskDto
{
    [Required(ErrorMessage = "Title is required")]
    [MaxLength(255, ErrorMessage = "Title must not exceed 255 characters")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "Content must not exceed 2000 characters")]
    public string? Content { get; set; }
}

/// <summary>
/// DTO для ответа с задачей
/// </summary>
public class TaskResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}