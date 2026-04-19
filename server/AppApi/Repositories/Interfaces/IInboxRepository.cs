// AppApi/Repositories/Interfaces/IInboxRepository.cs
using Common.Models;

namespace AppApi.Repositories.Interfaces;

public interface IInboxRepository
{
    Task<(IEnumerable<InboxItem> Items, bool HasOverflow)> GetAllAsync(string userId, int limit);
    Task<InboxItem?> GetByIdAsync(int id, string userId);
    Task<InboxItem> AddAsync(InboxItem item);
    Task<bool> SoftDeleteAsync(int id, string userId);
    Task<bool> RestoreAsync(int id, string userId);
}