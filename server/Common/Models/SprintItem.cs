using Common.Enums;

namespace Common.Models;

public class SprintItem
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public SprintStatus Status { get; set; } = SprintStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}