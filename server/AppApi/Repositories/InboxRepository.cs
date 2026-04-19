// AppApi/Repositories/InboxRepository.cs
using AppApi.Repositories.Interfaces;
using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly AppDbContext _context;

    public InboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<InboxItem> Items, bool HasOverflow)> GetAllAsync(string userId, int limit)
    {
        var query = _context.InboxItems
            .Where(i => i.UserId == userId && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt);

        // Проверяем, есть ли записи сверх лимита
        var totalCount = await query.CountAsync();
        var hasOverflow = totalCount > limit;

        var items = await query
            .Take(limit)
            .ToListAsync();

        return (items, hasOverflow);
    }

    public async Task<InboxItem?> GetByIdAsync(int id, string userId)
    {
        return await _context.InboxItems
            .FirstOrDefaultAsync(i => i.Id == id
                && i.UserId == userId
                && i.DeletedAt == null);
    }

    public async Task<InboxItem> AddAsync(InboxItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> SoftDeleteAsync(int id, string userId)
    {
        var item = await _context.InboxItems
            .FirstOrDefaultAsync(i => i.Id == id
                && i.UserId == userId
                && i.DeletedAt == null);

        if (item is null)
            return false;

        item.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int id, string userId)
    {
        var item = await _context.InboxItems
            .FirstOrDefaultAsync(i => i.Id == id
                && i.UserId == userId
                && i.DeletedAt != null);

        if (item is null)
            return false;

        item.DeletedAt = null;
        await _context.SaveChangesAsync();
        return true;
    }
}