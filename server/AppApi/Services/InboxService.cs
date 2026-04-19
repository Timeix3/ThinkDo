// AppApi/Services/InboxService.cs
using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;

namespace AppApi.Services;

public class InboxService : IInboxService
{
    private readonly IInboxRepository _repository;
    private const int DefaultLimit = 20;

    public InboxService(IInboxRepository repository)
    {
        _repository = repository;
    }

    public async Task<InboxListResponseDto> GetAllItemsAsync(string userId)
    {
        var (items, hasOverflow) = await _repository.GetAllAsync(userId, DefaultLimit);

        return new InboxListResponseDto
        {
            Items = items.Select(MapToDto),
            InboxOverflow = hasOverflow
        };
    }

    public async Task<InboxItemResponseDto> CreateItemAsync(CreateInboxItemDto dto, string userId)
    {
        // Валидация пустого title или только пробелов
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Title cannot be empty or whitespace");
        }

        var item = new InboxItem
        {
            Title = dto.Title.Trim(),
            UserId = userId
        };

        var created = await _repository.AddAsync(item);
        return MapToDto(created);
    }

    public async Task<bool> SoftDeleteItemAsync(int id, string userId)
    {
        return await _repository.SoftDeleteAsync(id, userId);
    }

    public async Task<bool> RestoreItemAsync(int id, string userId)
    {
        return await _repository.RestoreAsync(id, userId);
    }

    private static InboxItemResponseDto MapToDto(InboxItem item) => new()
    {
        Id = item.Id,
        Title = item.Title,
        CreatedAt = item.CreatedAt,
        UpdatedAt = item.UpdatedAt
    };
}