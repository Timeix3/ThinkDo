using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync(string userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetByIdAsync(int id, string userId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    }

    public async Task<TaskItem?> GetByDateAsync(DateTime date, string userId)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _context.Tasks
            .Where(t => t.UserId == userId && t.CreatedAt >= start && t.CreatedAt < end)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetAllByDateAsync(DateTime date, string userId)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _context.Tasks
            .Where(t => t.UserId == userId && t.CreatedAt >= start && t.CreatedAt < end)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem> AddAsync(TaskItem task)
    {
        task.CreatedAt = DateTime.UtcNow;
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> UpdateAsync(TaskItem task, string userId)
    {
        var existing = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == userId);

        if (existing is null)
            return null;

        existing.Title = task.Title;
        existing.Content = task.Content;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task is null)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }
}
