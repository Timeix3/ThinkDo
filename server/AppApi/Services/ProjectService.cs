using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;

namespace AppApi.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repository;
    private readonly ITaskRepository _taskRepository;
    public ProjectService(IProjectRepository repository, ITaskRepository taskRepository)
    {
        _repository = repository;
        _taskRepository = taskRepository;
    }


    public async Task<IEnumerable<ProjectResponseDto>> GetProjectsAsync(string userId)
    {
        await EnsureDefaultProjectExistsAsync(userId);

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

    public async Task<IEnumerable<TaskResponseDto>> GetProjectTasksAsync(int projectId, string userId)
    {
        var project = await _repository.GetByIdAsync(projectId, userId);

        if (project == null)
        {
            // Краевой случай: проект чужой, удален или не существует -> 404
            throw new KeyNotFoundException($"No project with ID {projectId} was found.");
        }

        var tasks = await _taskRepository.GetByProjectIdAsync(projectId, userId);

        return tasks.Select(t => new TaskResponseDto
        {
            Id = t.Id,
            Title = t.Title,
            Content = t.Content,
            Status = t.Status,
            ProjectId = t.ProjectId,
            CreatedAt = t.CreatedAt
        });
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

    private async Task EnsureDefaultProjectExistsAsync(string userId)
    {
        var defaultProject = await _repository.GetDefaultProjectAsync(userId, includeDeleted: true);

        if (defaultProject == null)
        {
            var newDefault = new ProjectItem
            {
                Name = "Текучка",
                Description = "Задачи без конкретного проекта",
                UserId = userId,
                IsDefault = true
            };

            try
            {
                await _repository.AddAsync(newDefault);
            }
            catch (Exception)
            {
                // Если сработал уникальный индекс (гонка потоков), просто игнорируем ошибку
            }
        }
        else if (defaultProject.DeletedAt != null)
        {
            defaultProject.DeletedAt = null;
            defaultProject.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(defaultProject);
        }
    }
}