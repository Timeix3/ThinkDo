using Common.Models;

namespace  AppApi.Repositories.Interfaces;

public interface IProjectRepository
{
    Task<IEnumerable<ProjectItem>> GetAllAsync(string userId);
    Task<ProjectItem?> GetByIdAsync(int id, string userId, bool includeTasks = false);
    Task<ProjectItem> AddAsync(ProjectItem project);
    Task UpdateAsync(ProjectItem project);
}