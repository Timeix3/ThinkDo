using System.ComponentModel.DataAnnotations;

namespace AppApi.Models.DTOs;

/// <summary>
/// Данные для создания нового проекта
/// </summary>
public class CreateProjectDto
{
    [Required(ErrorMessage = "The project name is required to fill in.")]
    [MaxLength(200, ErrorMessage = "The project name cannot exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;
    [MaxLength(2000, ErrorMessage = "The description cannot exceed 2000 characters.")]
    public string? Description { get; set; }
}

/// <summary>
/// Данные для обновления существующего проекта
/// </summary>
public class UpdateProjectDto
{
    [Required(ErrorMessage = "The project name is required to fill in.")]
    [MaxLength(200, ErrorMessage = "The project name cannot exceed 200 characters.")]

    public string Name { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "The description cannot exceed 2000 characters.")]
    public string? Description { get; set; }
}

/// <summary>
/// Полная информация о проекте для ответа API
/// </summary>
public class ProjectResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public IEnumerable<TaskResponseDto>? Tasks { get; set; }
}