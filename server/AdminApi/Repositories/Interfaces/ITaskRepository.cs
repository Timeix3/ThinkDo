using Common.Models;

namespace AdminApi.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync(string userId);
    Task<TaskItem?> GetByIdAsync(int id, string userId);
    Task<TaskItem> AddAsync(TaskItem task);
    Task<TaskItem?> UpdateAsync(TaskItem task, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}