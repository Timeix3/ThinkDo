using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class FlowPhaseRepository : IFlowPhaseRepository
{
    private readonly AppDbContext _context;

    public FlowPhaseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserFlowPhase?> GetByUserIdAsync(string userId)
    {
        return await _context.UserFlowPhases
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task UpsertAsync(string userId, string phase)
    {
        var existing = await _context.UserFlowPhases.FirstOrDefaultAsync(x => x.UserId == userId);

        if (existing is null)
        {
            _context.UserFlowPhases.Add(new UserFlowPhase
            {
                UserId = userId,
                Phase = phase,
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Phase = phase;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
