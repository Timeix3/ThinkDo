using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using Common.Models;

namespace AppApi.Services.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string userId);
    Task<ProjectResponseDto?> GetProjectByIdAsync(int id, string userId);
    Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, string userId);
    Task<ProjectResponseDto?> UpdateProjectAsync(int id, UpdateProjectDto dto, string userId);
    Task<bool> DeleteProjectAsync(int id, string userId);
    ProjectResponseDto MapToDto(ProjectItem p, bool includeTasks = false) => new();
    Task<IEnumerable<TaskResponseDto>> GetProjectTasksAsync(int projectId, string userId);
    Task<PlanningResponseDto> GetPlanningProjectsAsync(string userId);
}