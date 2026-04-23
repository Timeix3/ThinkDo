using System.ComponentModel.DataAnnotations;
using Common.Models;
using Common.Enums;

namespace AppApi.Models.DTOs;

/// <summary>
/// DTO для создания рутины
/// </summary>
public class CreateRoutineDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255, ErrorMessage = "Name must not exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Frequency is required")]
    [EnumDataType(typeof(RoutineFrequency), ErrorMessage = "Frequency must be 'daily', 'weekly', or 'monthly'")]
    public RoutineFrequency Frequency { get; set; }
}

/// <summary>
/// DTO для обновления рутины
/// </summary>
public class UpdateRoutineDto
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255, ErrorMessage = "Name must not exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Frequency is required")]
    [EnumDataType(typeof(RoutineFrequency), ErrorMessage = "Frequency must be 'daily', 'weekly', or 'monthly'")]
    public RoutineFrequency Frequency { get; set; }
}

/// <summary>
/// DTO для ответа с рутиной
/// </summary>
public class RoutineResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoutineFrequency Frequency { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}