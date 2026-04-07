using Common.Models;

namespace AdminApi.Repositories.Interfaces;

public interface ITaskRepository
{
    Task<IEnumerable<TaskItem>> GetAllAsync();
Task<TaskItem?> GetByIdAsync(int id);
}