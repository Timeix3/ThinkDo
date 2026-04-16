using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;

namespace AppApi.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(string userId)
    {
        var tasks = await _repository.GetAllAsync(userId);
        return tasks.Select(MapToDto);
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id, string userId)
    {
        var task = await _repository.GetByIdAsync(id, userId);
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
        var task = new TaskItem
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId
        };

        var created = await _repository.AddAsync(task);
        return MapToDto(created);
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, string userId)
    {
        var task = new TaskItem
        {
            Id = id,
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId
        };

        var updated = await _repository.UpdateAsync(task, userId);
        return updated is null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteTaskAsync(int id, string userId)
    {
        return await _repository.DeleteAsync(id, userId);
    }

    private static TaskResponseDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Content = task.Content,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
