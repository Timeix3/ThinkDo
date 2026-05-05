namespace AppApi.Services.Interfaces;

public interface IFlowPhaseService
{
    Task<string> GetPhaseAsync(string userId);
    Task<string> SetPhaseAsync(string userId, string phase);
}
