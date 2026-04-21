using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;

namespace AppApi.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    public ProjectService(IProjectRepository repository) => _repository = repository;

    public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string userId)
    {
        var projects = await _repository.GetAllAsync(userId);
        return projects.Select(p => MapToDto(p));
    }

    public async Task<ProjectResponseDto?> GetProjectByIdAsync(int id, string userId)
    {
        var project = await _repository.GetByIdAsync(id, userId, includeTasks: true);
        return project == null ? null : MapToDto(project, includeTasks: true);
    }

    public async Task<ProjectResponseDto> CreateProjectAsync(CreateProjectDto dto, string userId)
    {
        var project = new ProjectItem 
        { 
            Name = dto.Name.Trim(), 
            Description = dto.Description, 
            UserId = userId 
        };
        var result = await _repository.AddAsync(project);
        return MapToDto(result);
    }

    public async Task<ProjectResponseDto?> UpdateProjectAsync(int id, UpdateProjectDto dto, string userId)
    {
        var project = await _repository.GetByIdAsync(id, userId);
        if (project == null) return null;

        if (project.IsDefault && project.Name != dto.Name.Trim())
            throw new InvalidOperationException("Название системного проекта 'Текучка' нельзя изменить.");

        project.Name = dto.Name.Trim();
        project.Description = dto.Description;
        await _repository.UpdateAsync(project);
        return MapToDto(project);
    }

    public async Task<bool> DeleteProjectAsync(int id, string userId)
    {
        var project = await _repository.GetByIdAsync(id, userId);
        if (project == null) return false;

        if (project.IsDefault)
            throw new InvalidOperationException("Проект 'Текучка' является системным и не может быть удален.");

        project.DeletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(project);
        return true;
    }

    private static ProjectResponseDto MapToDto(ProjectItem p, bool includeTasks = false) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        IsDefault = p.IsDefault,
        CreatedAt = p.CreatedAt,
        Tasks = includeTasks ? p.Tasks.Select(t => new TaskResponseDto 
        { 
            Id = t.Id, 
            Title = t.Title, 
            Content = t.Content, 
            Status = t.Status,
            CreatedAt = t.CreatedAt 
        }) : null
    };
}