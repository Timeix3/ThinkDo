using AppApi.Services.Interfaces;
using AppApi.Repositories.Interfaces;

namespace AppApi.Services;

public class FlowPhaseService : IFlowPhaseService
{
    private static readonly HashSet<string> AllowedPhases = new(StringComparer.OrdinalIgnoreCase)
    {
        "sprint", "review", "planning"
    };

    private readonly IFlowPhaseRepository _flowPhaseRepository;

    public FlowPhaseService(IFlowPhaseRepository flowPhaseRepository)
    {
        _flowPhaseRepository = flowPhaseRepository;
    }

    public async Task<string> GetPhaseAsync(string userId)
    {
        var record = await _flowPhaseRepository.GetByUserIdAsync(userId);
        return record?.Phase ?? "planning";
    }

    public async Task<string> SetPhaseAsync(string userId, string phase)
    {
        if (string.IsNullOrWhiteSpace(phase) || !AllowedPhases.Contains(phase))
            throw new ArgumentException("Invalid phase");

        var normalized = phase.ToLowerInvariant();
        await _flowPhaseRepository.UpsertAsync(userId, normalized);
        return normalized;
    }
}
