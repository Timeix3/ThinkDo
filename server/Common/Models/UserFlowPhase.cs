namespace Common.Models;

public class UserFlowPhase
{
    public string UserId { get; set; } = string.Empty;
    public string Phase { get; set; } = "planning";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
