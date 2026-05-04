using System.ComponentModel.DataAnnotations;

namespace AppApi.Models.DTOs;

/// <summary>
/// Текущий статус спринта пользователя
/// </summary>
public class SprintStatusDto
{
    public bool HasActiveSprint { get; set; }
    public int PendingTasksCount { get; set; }
    public int InboxCount { get; set; }
    public string Phase { get; set; } = "review"; // "sprint" | "review" | "planning"
}

/// <summary>
/// Запрос на старт спринта
/// </summary>
public class StartSprintRequestDto
{
    [Required]
    public List<int> TaskIds { get; set; } = new();
}

/// <summary>
/// Ответ при успешном старте
/// </summary>
public class StartSprintResponseDto
{
    public bool Success { get; set; }
    public int SprintId { get; set; }
    public int TasksCount { get; set; }
}

/// <summary>
/// Ответ при завершении спринта
/// </summary>
public class CompleteSprintResponseDto
{
    public bool Success { get; set; }
    public string NextPhase { get; set; } = "review";
}