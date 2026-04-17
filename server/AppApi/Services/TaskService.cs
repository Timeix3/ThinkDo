using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Enums;
using Common.Models;

namespace AppApi.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _repository;

    public TaskService(ITaskRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskListResponseDto> GetAllTasksAsync(string userId, int offset = 0, int limit = 50)
    {
        var (items, totalCount) = await _repository.GetAllAsync(userId, offset, limit);

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
        var task = await _repository.GetByIdAsync(id, userId);
        return task is null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, string userId)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Content = dto.Content,
            UserId = userId,
            Status = dto.Status,
            BlockedByTaskId = dto.BlockedByTaskId
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
            UserId = userId,
            Status = dto.Status ?? default,
            BlockedByTaskId = dto.BlockedByTaskId
        };

        var updated = await _repository.UpdateAsync(task, userId);
        return updated is null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteTaskAsync(int id, string userId)
    {
        return await _repository.SoftDeleteAsync(id, userId);
    }

    public async Task<TaskResponseDto?> CompleteTaskAsync(int id, string userId)
    {
        var task = await _repository.GetByIdAsync(id, userId);

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
        var task = await _repository.GetByIdAsync(id, userId);

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

    private static TaskResponseDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Content = task.Content,
        Status = task.Status,
        BlockedByTaskId = task.BlockedByTaskId,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}