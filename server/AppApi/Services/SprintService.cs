using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Enums;
using Common.Models;

namespace AppApi.Services;

public class SprintService : ISprintService
{
    private readonly ISprintRepository _repository;
    private readonly ITaskRepository _taskRepository;
    private readonly IInboxRepository _inboxRepository;

    public SprintService(ISprintRepository repository, ITaskRepository taskRepository, IInboxRepository inboxRepository)
    {
        _repository = repository;
        _taskRepository = taskRepository;
        _inboxRepository = inboxRepository;
    }

    public async Task<SprintStatusDto> GetStatusAsync(string userId)
    {
        var activeSprint = await _repository.GetActiveSprintAsync(userId);
        var inboxCount = await _inboxRepository.GetCountAsync(userId);

        int pendingCount = activeSprint?.Tasks.Count(t => t.Status != TasksStatus.Completed) ?? 0;
        bool hasActive = activeSprint != null;

        string phase = "planning";
        if (hasActive && pendingCount > 0) phase = "sprint";
        else if (inboxCount > 0) phase = "review";

        return new SprintStatusDto
        {
            HasActiveSprint = hasActive,
            PendingTasksCount = pendingCount,
            InboxCount = inboxCount,
            Phase = phase
        };
    }

    public async Task<StartSprintResponseDto> StartSprintAsync(List<int> taskIds, string userId)
    {
        var active = await _repository.GetActiveSprintAsync(userId);
        if (active != null) throw new InvalidOperationException("Спринт уже запущен.");

        var tasks = await _taskRepository.GetByIdsAsync(taskIds, userId);
        if (tasks.Count == 0) throw new ArgumentException("Список задач пуст.");

        var sprint = new SprintItem { UserId = userId, Status = SprintStatus.Active, Tasks = tasks };
        await _repository.AddAsync(sprint);

        return new StartSprintResponseDto { Success = true, SprintId = sprint.Id, TasksCount = tasks.Count };
    }

    public async Task<CompleteSprintResponseDto> CompleteSprintAsync(string userId)
    {
        var active = await _repository.GetActiveSprintAsync(userId);
        if (active == null) throw new KeyNotFoundException("Активный спринт не найден.");

        active.Status = SprintStatus.Completed;
        active.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(active);

        return new CompleteSprintResponseDto { Success = true, NextPhase = "review" };
    }
}