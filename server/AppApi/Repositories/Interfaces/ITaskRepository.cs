using Common.Models;
using Common.Enums;

namespace AppApi.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetAllAsync(string userId, int offset = 0, int limit = 50);
    Task<TaskItem?> GetByIdAsync(int id, string userId);
    Task<TaskItem?> GetByDateAsync(DateTime date, string userId);
    Task<IEnumerable<TaskItem>> GetAllByDateAsync(DateTime date, string userId);
    Task<TaskItem> AddAsync(TaskItem task);
    Task<TaskItem?> UpdateAsync(TaskItem task, string userId);
    Task<bool> SoftDeleteAsync(int id, string userId);
    Task<TaskItem?> UpdateStatusAsync(int id, string userId, TasksStatus newStatus);
    Task<TaskItem?> UpdateSprintSelectionAsync(int id, string userId, bool isSelectedForSprint);
    Task<IEnumerable<TaskItem>> GetSprintTasksAsync(string userId);
    Task<IEnumerable<TaskItem>> GetBlockedByTaskIdAsync(int taskId, string userId);
    Task<IEnumerable<TaskItem>> GetByProjectIdAsync(int projectId, string userId);
}