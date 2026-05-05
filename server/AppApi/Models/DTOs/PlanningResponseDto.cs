using Common.Enums;

namespace AppApi.Models.DTOs;

public class PlanningResponseDto
{
    public IEnumerable<PlanningProjectDto> Projects { get; set; } = Array.Empty<PlanningProjectDto>();
    public int TotalProjects { get; set; }
}

public class PlanningProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IEnumerable<PlanningTaskDto> Tasks { get; set; } = Array.Empty<PlanningTaskDto>();
}

public class PlanningTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TasksStatus Status { get; set; }
    public bool Selected { get; set; }
}