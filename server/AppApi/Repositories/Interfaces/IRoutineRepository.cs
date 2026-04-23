using Common.Models;

namespace AppApi.Repositories.Interfaces;

public interface IRoutineRepository
{
    Task<IEnumerable<Routine>> GetAllAsync(string userId);
    Task<Routine?> GetByIdAsync(int id, string userId);
    Task<Routine> AddAsync(Routine routine);
    Task<Routine?> UpdateAsync(Routine routine, string userId);
    Task<bool> SoftDeleteAsync(int id, string userId);
}