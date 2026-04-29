// AppApi/Services/InboxClassificationService.cs
using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Enums;
using Common.Models;
using Microsoft.Extensions.Logging;

namespace AppApi.Services;

public class InboxClassificationService : IInboxClassificationService
{
    private readonly IInboxRepository _inboxRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IRoutineRepository _routineRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<InboxClassificationService> _logger;

    public InboxClassificationService(
        IInboxRepository inboxRepository,
        ITaskRepository taskRepository,
        IRoutineRepository routineRepository,
        IProjectRepository? projectRepository = null,
        ILogger<InboxClassificationService> logger = null!)
    {
        _inboxRepository = inboxRepository;
        _taskRepository = taskRepository;
        _routineRepository = routineRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<ClassifyInboxItemResponseDto> ClassifyInboxItemAsync(
        int inboxItemId,
        ClassifyInboxItemDto request,
        string userId)
    {
        // 1. Валидация targetType
        var targetType = request.TargetType.ToLowerInvariant();
        var validTypes = new[] { "task", "project", "routine" };

        if (!validTypes.Contains(targetType))
        {
            throw new ArgumentException($"Invalid targetType: '{request.TargetType}'. Must be one of: {string.Join(", ", validTypes)}");
        }

        // 2. Проверяем существование и состояние записи инбокса
        var inboxItem = await _inboxRepository.GetByIdAsync(inboxItemId, userId);

        if (inboxItem is null)
        {
            throw new KeyNotFoundException($"Inbox item with id '{inboxItemId}' not found");
        }

        // Проверяем, не удалена ли уже запись (параллельная классификация)
        if (inboxItem.DeletedAt.HasValue)
        {
            throw new InvalidOperationException($"Inbox item with id '{inboxItemId}' has already been classified or deleted");
        }

        // 3. Создаём целевую сущность в зависимости от типа
        try
        {
            var result = targetType switch
            {
                "task" => await ClassifyAsTaskAsync(inboxItem, request, userId),
                "project" => await ClassifyAsProjectAsync(inboxItem, request, userId),
                "routine" => await ClassifyAsRoutineAsync(inboxItem, request, userId),
                _ => throw new ArgumentException($"Unsupported target type: {targetType}")
            };

            // 4. Soft-delete запись инбокса
            var deleted = await _inboxRepository.SoftDeleteAsync(inboxItemId, userId);

            if (!deleted)
            {
                _logger.LogWarning(
                    "Failed to soft-delete inbox item {InboxItemId} after classification to {TargetType}",
                    inboxItemId, targetType);

                // Запись могла быть удалена параллельно — это нормально
                // Продолжаем, так как сущность уже создана
            }

            _logger.LogInformation(
                "Successfully classified inbox item {InboxItemId} to {TargetType} with id {EntityId}",
                inboxItemId, targetType, result.Id);

            return result;
        }
        catch (ArgumentException)
        {
            // Пробрасываем ArgumentException как есть (ошибки валидации)
            throw;
        }
        catch (Exception ex) when (ex is not KeyNotFoundException && ex is not InvalidOperationException)
        {
            _logger.LogError(ex,
                "Error classifying inbox item {InboxItemId} to {TargetType}",
                inboxItemId, targetType);

            throw new InvalidOperationException(
                $"Failed to classify inbox item: {ex.Message}", ex);
        }
    }

    private async Task<ClassifyInboxItemResponseDto> ClassifyAsTaskAsync(
        InboxItem inboxItem,
        ClassifyInboxItemDto request,
        string userId)
    {
        var createTaskDto = request.ToCreateTaskDto();

        var task = new TaskItem
        {
            Title = createTaskDto.Title,
            Content = createTaskDto.Content ?? inboxItem.Title, // Используем title инбокса как контент если не указан
            UserId = userId,
            ProjectId = createTaskDto.ProjectId
        };

        var created = await _taskRepository.AddAsync(task);

        return new ClassifyInboxItemResponseDto
        {
            Id = created.Id,
            TargetType = "task",
            Title = created.Title,
            Status = created.Status.ToString(),
            CreatedAt = created.CreatedAt
        };
    }

    private async Task<ClassifyInboxItemResponseDto> ClassifyAsProjectAsync(
        InboxItem inboxItem,
        ClassifyInboxItemDto request,
        string userId)
    {
        var createProjectDto = request.ToCreateProjectDto();

        var project = new ProjectItem
        {
            Name = createProjectDto.Name,
            Description = createProjectDto.Description,
            UserId = userId,
            IsDefault = false
        };

        var created = await _projectRepository.AddAsync(project);

        return new ClassifyInboxItemResponseDto
        {
            Id = created.Id,
            TargetType = "project",
            Title = created.Name,
            Description = created.Description,
            CreatedAt = created.CreatedAt
        };
    }

    private async Task<ClassifyInboxItemResponseDto> ClassifyAsRoutineAsync(
        InboxItem inboxItem,
        ClassifyInboxItemDto request,
        string userId)
    {
        var createRoutineDto = request.ToCreateRoutineDto();

        var routine = new Routine
        {
            Name = createRoutineDto.Name,
            Frequency = createRoutineDto.Frequency,
            UserId = userId
        };

        var created = await _routineRepository.AddAsync(routine);

        return new ClassifyInboxItemResponseDto
        {
            Id = created.Id,
            TargetType = "routine",
            Title = created.Name,
            Frequency = created.Frequency.ToString(),
            CreatedAt = created.CreatedAt
        };
    }
}