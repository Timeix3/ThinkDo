using Common.Models;

namespace AppApi.Repositories.Interfaces;

public interface IFlowPhaseRepository
{
    Task<UserFlowPhase?> GetByUserIdAsync(string userId);
    Task UpsertAsync(string userId, string phase);
}
