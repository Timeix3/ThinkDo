using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetAllAsync(string userId, int offset = 0, int limit = 50)
    {
        var query = _context.Tasks
            .Where(t => t.UserId == userId && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, string userId)
    {
        return await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null);
    }

    public async Task<TaskItem?> GetByDateAsync(DateTime date, string userId)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _context.Tasks
            .Where(t => t.UserId == userId && t.CreatedAt >= start && t.CreatedAt < end && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetAllByDateAsync(DateTime date, string userId)
    {
        var start = date.Date;
        var end = start.AddDays(1);

        return await _context.Tasks
            .Where(t => t.UserId == userId && t.CreatedAt >= start && t.CreatedAt < end && t.DeletedAt == null)
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
            .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == userId && t.DeletedAt == null);

        if (existing is null)
            return null;

        existing.Title = task.Title;
        existing.Content = task.Content;

        if (task.Status != default)
            existing.Status = task.Status;

        if (task.BlockedByTaskId.HasValue)
            existing.BlockedByTaskId = task.BlockedByTaskId;

        existing.ProjectId = task.ProjectId;

        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> SoftDeleteAsync(int id, string userId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null);

        if (task is null)
            return false;

        task.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TaskItem?> UpdateStatusAsync(int id, string userId, TasksStatus newStatus)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null);

        if (task is null)
            return null;

        task.Status = newStatus;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<TaskItem?> UpdateSprintSelectionAsync(int id, string userId, bool isSelectedForSprint)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId && t.DeletedAt == null);

        if (task is null)
            return null;

        task.IsSelectedForSprint = isSelectedForSprint;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return task;
    }

    public async Task<IEnumerable<TaskItem>> GetSprintTasksAsync(string userId)
    {
        return await _context.Tasks
            .Where(t => t.UserId == userId
                     && t.DeletedAt == null
                     && t.IsSelectedForSprint)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetBlockedByTaskIdAsync(int taskId, string userId)
    {
        return await _context.Tasks
            .Where(t => t.BlockedByTaskId == taskId && t.UserId == userId && t.DeletedAt == null)
            .ToListAsync();
    }

    public async Task<IEnumerable<TaskItem>> GetByProjectIdAsync(int projectId, string userId)
    {
        return await _context.Tasks
            .Where(t => t.ProjectId == projectId
                     && t.UserId == userId
                     && t.DeletedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<TaskItem>> GetByIdsAsync(List<int> ids, string userId)
    {
        return await _context.Tasks
            .Where(t => ids.Contains(t.Id) && t.UserId == userId && t.DeletedAt == null)
            .ToListAsync();
    }
}