using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;

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
        IProjectRepository projectRepository,
        ILogger<InboxClassificationService> logger)
    {
        _inboxRepository = inboxRepository;
        _taskRepository = taskRepository;
        _routineRepository = routineRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<ClassifyResponseDto> ClassifyInboxItemAsync(int inboxItemId, ClassifyInboxItemDto request, string userId)
    {
        // 1. Валидация режима и типа
        string mode = request.Mode?.ToLowerInvariant() ?? "convert";
        if (mode != "convert" && mode != "create")
            throw new ArgumentException("Invalid mode. Use 'convert' or 'create'.");

        string entityType = request.EntityType.ToLowerInvariant();

        // 2. Проверка инбокса
        var inboxItem = await _inboxRepository.GetByIdAsync(inboxItemId, userId);
        if (inboxItem == null || inboxItem.DeletedAt.HasValue)
            throw new KeyNotFoundException("Inbox item not found or already deleted.");

        // 3. Создание сущности
        int createdId = entityType switch
        {
            "task" => (await CreateTaskInternal(request, userId)).Id,
            "project" => (await CreateProjectInternal(request, userId)).Id,
            "routine" => (await CreateRoutineInternal(request, userId)).Id,
            _ => throw new ArgumentException($"Unsupported entity type: {entityType}")
        };

        // 4. Логика режима
        bool wasDeleted = false;
        if (mode == "convert")
        {
            wasDeleted = await _inboxRepository.SoftDeleteAsync(inboxItemId, userId);
            _logger.LogInformation("Inbox item {Id} converted and deleted", inboxItemId);
        }
        else
        {
            _logger.LogInformation("Inbox item {Id} used to create entity {Type} {CreatedId}, but kept in list", 
                inboxItemId, entityType, createdId);
        }

        return new ClassifyResponseDto
        {
            Success = true,
            CreatedEntityId = createdId,
            InboxDeleted = wasDeleted
        };
    }

    private async Task<TaskItem> CreateTaskInternal(ClassifyInboxItemDto req, string uid)
    {
        var dto = req.ToCreateTaskDto();
        return await _taskRepository.AddAsync(new TaskItem 
        { 
            Title = dto.Title, Content = dto.Content, UserId = uid, ProjectId = dto.ProjectId 
        });
    }

    private async Task<ProjectItem> CreateProjectInternal(ClassifyInboxItemDto req, string uid)
    {
        var dto = req.ToCreateProjectDto();
        return await _projectRepository.AddAsync(new ProjectItem 
        { 
            Name = dto.Name, Description = dto.Description, UserId = uid 
        });
    }

    private async Task<Routine> CreateRoutineInternal(ClassifyInboxItemDto req, string uid)
    {
        var dto = req.ToCreateRoutineDto();
        return await _routineRepository.AddAsync(new Routine 
        { 
            Name = dto.Name, Frequency = dto.Frequency, UserId = uid 
        });
    }
}