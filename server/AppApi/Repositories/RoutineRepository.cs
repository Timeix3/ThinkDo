using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class RoutineRepository : IRoutineRepository
{
    private readonly AppDbContext _context;

    public RoutineRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Routine>> GetAllAsync(string userId)
    {
        return await _context.Routines
            .Where(r => r.UserId == userId && r.DeletedAt == null)
            .OrderBy(r => r.Name) // или OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Routine?> GetByIdAsync(int id, string userId)
    {
        return await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == id
                && r.UserId == userId
                && r.DeletedAt == null);
    }

    public async Task<Routine> AddAsync(Routine routine)
    {
        routine.CreatedAt = DateTime.UtcNow;
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();
        return routine;
    }

    public async Task<Routine?> UpdateAsync(Routine routine, string userId)
    {
        var existing = await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == routine.Id
                && r.UserId == userId
                && r.DeletedAt == null);

        if (existing is null)
            return null;

        existing.Name = routine.Name;
        existing.Frequency = routine.Frequency;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> SoftDeleteAsync(int id, string userId)
    {
        var routine = await _context.Routines
            .FirstOrDefaultAsync(r => r.Id == id
                && r.UserId == userId
                && r.DeletedAt == null);

        if (routine is null)
            return false;

        routine.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}