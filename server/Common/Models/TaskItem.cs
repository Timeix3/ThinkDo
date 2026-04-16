using Common.Enums;

namespace Common.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string UserId { get; set; } = string.Empty;
    public TasksStatus Status { get; set; } = TasksStatus.Available;
    public int? BlockedByTaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}