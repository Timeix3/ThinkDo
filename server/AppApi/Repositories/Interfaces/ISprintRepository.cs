using Common.Models;
using Common.Enums;

namespace AppApi.Repositories.Interfaces;

public interface ISprintRepository
{
    Task<SprintItem?> GetActiveSprintAsync(string userId);
    Task AddAsync(SprintItem sprint);
    Task UpdateAsync(SprintItem sprint);
    
}