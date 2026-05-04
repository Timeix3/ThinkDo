using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class SprintRepository : ISprintRepository 
{
    private readonly AppDbContext _context;
    public SprintRepository(AppDbContext context) => _context = context;

    public async Task<SprintItem?> GetActiveSprintAsync(string userId)
    {
        return await _context.Sprints
            .Include(s => s.Tasks)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == SprintStatus.Active);
    }

    public async Task AddAsync(SprintItem sprint)
    {
        await _context.Sprints.AddAsync(sprint);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SprintItem sprint)
    {
        _context.Sprints.Update(sprint);
        await _context.SaveChangesAsync();
    }
}