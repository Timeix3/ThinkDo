using AdminApi.Models.DTOs;
using AdminApi.Repositories.Interfaces;
using AdminApi.Services.Interfaces;
using Common.Models;

namespace AdminApi.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
    {
        var tasks = await _repository.GetAllAsync();
        return tasks.Select(MapToDto);
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
    {
        var task = await _repository.GetByIdAsync(id);
        return task is null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Content = dto.Content,
        };

        var created = await _repository.AddAsync(task);
        return MapToDto(created);
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto)
    {
        var task = new TaskItem
        {
            Id = id,
            Title = dto.Title,
            Content = dto.Content,
        };

        var updated = await _repository.UpdateAsync(task);
        return updated is null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        return await _repository.DeleteAsync(id);
    }

    private static TaskResponseDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Content = task.Content,
    };
}