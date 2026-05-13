using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Enums;
using Common.Models;

namespace AppApi.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;
    private readonly IProjectRepository _projectRepository;

    public TaskService(ITaskRepository repository, IProjectRepository projectRepository)
    {
        _repository = repository;
        _projectRepository = projectRepository;
    }

    public async Task<TaskListResponseDto> GetAllTasksAsync(string userId, int offset = 0, int limit = 50)
    {
        var (items, totalCount) = await _repository.GetAllWithProjectAsync(userId, offset, limit);

        return new TaskListResponseDto
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            PageSize = limit,
            PageNumber = offset / limit + 1,
            HasMore = (offset + limit) < totalCount
        };
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id, string userId)
    {
        var task = await _repository.GetByIdWithProjectAsync(id, userId);
        return task is null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto?> GetTodayTaskAsync(string userId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var task = await _repository.GetByDateAsync(todayUtc, userId);
        return task is null ? null : MapToDto(task);
    }

    public async Task<IEnumerable<TaskResponseDto>> GetTodayTasksAsync(string userId)
    {
        var todayUtc = DateTime.UtcNow.Date;
        var tasks = await _repository.GetAllByDateAsync(todayUtc, userId);
        return tasks.Select(MapToDto);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, string userId)
    {
        string? projectName = null;
        string? projectDescription = null;

        if (dto.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value, userId);

            if (project == null)
            {
                throw new KeyNotFoundException($"The specified project with ID {dto.ProjectId} does not exist or has been deleted.");
            }

            projectName = project.Name;
            projectDescription = project.Description;
        }

        var task = new TaskItem
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId,
            Status = dto.Status,
            BlockedByTaskId = dto.BlockedByTaskId,
            ProjectId = dto.ProjectId
        };

        var created = await _repository.AddAsync(task);

        var response = MapToDto(created);
        response.Project = dto.ProjectId.HasValue
            ? new ProjectInfoDto
            {
                Id = dto.ProjectId.Value,
                Name = projectName ?? "Stub",
                Description = projectDescription
            }
            : null;

        return response;
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, string userId)
    {
        if (dto.ProjectId.HasValue)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId.Value, userId);
            if (project == null)
                throw new ArgumentException("The project is not available.");
        }

        var task = new TaskItem
        {
            Id = id,
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId,
            Status = dto.Status ?? default,
            BlockedByTaskId = dto.BlockedByTaskId,
            ProjectId = dto.ProjectId
        };

        var updated = await _repository.UpdateAsync(task, userId);

        if (updated is null)
            return null;

        // Если есть проект, получаем его данные
        if (updated.ProjectId.HasValue)
        {
            var withProject = await _repository.GetByIdWithProjectAsync(updated.Id, userId);
            return withProject is null ? null : MapToDto(withProject);
        }

        return MapToDto(updated);
    }

    public async Task<bool> DeleteTaskAsync(int id, string userId)
    {
        return await _repository.SoftDeleteAsync(id, userId);
    }

    public async Task<TaskResponseDto?> CompleteTaskAsync(int id, string userId)
    {
        var task = await _repository.GetByIdWithProjectAsync(id, userId);

        if (task is null)
            return null;

        // Проверка: нельзя завершить уже завершённую задачу (идемпотентность)
        if (task.Status == TasksStatus.Completed)
            return MapToDto(task);

        // Проверка: нельзя завершить отменённую задачу
        if (task.Status == TasksStatus.Cancelled)
            throw new InvalidOperationException("Cannot complete a cancelled task");

        // Проверка: нельзя завершить заблокированную задачу
        if (task.Status == TasksStatus.Blocked)
            throw new InvalidOperationException("Cannot complete a blocked task");

        // Обновляем статус задачи на Completed
        var completed = await _repository.UpdateStatusAsync(id, userId, TasksStatus.Completed);

        if (completed is null)
            return null;

        // Убираем задачу из спринта при завершении
        if (completed.IsSelectedForSprint)
        {
            await _repository.UpdateSprintSelectionAsync(id, userId, false);
            completed.IsSelectedForSprint = false;
        }

        // Каскадная разблокировка: находим все задачи, заблокированные текущей
        var blockedTasks = await _repository.GetBlockedByTaskIdAsync(id, userId);

        foreach (var blockedTask in blockedTasks)
        {
            // Разблокируем задачи, переводя их в Available
            await _repository.UpdateStatusAsync(blockedTask.Id, userId, TasksStatus.Available);
        }

        return MapToDto(completed);
    }

    public async Task<TaskResponseDto?> CancelTaskAsync(int id, string userId)
    {
        var task = await _repository.GetByIdWithProjectAsync(id, userId);

        if (task is null)
            return null;

        // Проверка: нельзя отменить уже отменённую задачу (идемпотентность)
        if (task.Status == TasksStatus.Cancelled)
            return MapToDto(task);

        // Проверка: нельзя отменить завершённую задачу
        if (task.Status == TasksStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed task");

        // Обновляем статус задачи на Cancelled
        var cancelled = await _repository.UpdateStatusAsync(id, userId, TasksStatus.Cancelled);

        // ВАЖНО: отмена задачи НЕ разблокирует зависимые задачи
        // Они остаются в статусе Blocked

        return cancelled is null ? null : MapToDto(cancelled);
    }

    public async Task<TaskResponseDto?> SelectTaskForSprintAsync(int id, string userId)
    {
        var task = await _repository.GetByIdWithProjectAsync(id, userId);

        if (task is null)
            return null;

        if (task.Status == TasksStatus.Completed)
            throw new InvalidOperationException("Cannot select a completed task for sprint");

        if (task.Status == TasksStatus.Blocked)
            throw new InvalidOperationException("Cannot select a blocked task for sprint");

        if (task.IsSelectedForSprint)
            return MapToDto(task);

        var selected = await _repository.UpdateSprintSelectionAsync(id, userId, true);
        return selected is null ? null : MapToDto(selected);
    }

    public async Task<TaskResponseDto?> DeselectTaskForSprintAsync(int id, string userId)
    {
        var task = await _repository.GetByIdWithProjectAsync(id, userId);

        if (task is null)
            return null;

        if (!task.IsSelectedForSprint)
            return MapToDto(task);

        var deselected = await _repository.UpdateSprintSelectionAsync(id, userId, false);
        return deselected is null ? null : MapToDto(deselected);
    }

    public async Task<IEnumerable<TaskResponseDto>> GetSprintTasksAsync(string userId)
    {
        var tasks = await _repository.GetSprintTasksAsync(userId);
        return tasks.Select(MapToDto);
    }

    private static TaskResponseDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Content = task.Content,
        Status = task.Status,
        IsSelectedForSprint = task.IsSelectedForSprint,
        BlockedByTaskId = task.BlockedByTaskId,
        ProjectId = task.ProjectId,
        Project = task.Project is not null && task.Project.DeletedAt == null
            ? new ProjectInfoDto
            {
                Id = task.Project.Id,
                Name = task.Project.Name,
                Description = task.Project.Description
            }
            : null,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}