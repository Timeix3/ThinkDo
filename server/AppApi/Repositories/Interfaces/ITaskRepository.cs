using Common.Models;

namespace AppApi.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync(string userId);
    Task<TaskItem?> GetByIdAsync(int id, string userId);
    Task<TaskItem?> GetByDateAsync(DateTime date, string userId);
    Task<IEnumerable<TaskItem>> GetAllByDateAsync(DateTime date, string userId);
    Task<TaskItem> AddAsync(TaskItem task);
    Task<TaskItem?> UpdateAsync(TaskItem task, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}
