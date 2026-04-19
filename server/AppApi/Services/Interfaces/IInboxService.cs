// AppApi/Services/Interfaces/IInboxService.cs
using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface IInboxService
{
    Task<InboxListResponseDto> GetAllItemsAsync(string userId);
    Task<InboxItemResponseDto> CreateItemAsync(CreateInboxItemDto dto, string userId);
    Task<bool> SoftDeleteItemAsync(int id, string userId);
    Task<bool> RestoreItemAsync(int id, string userId);
}