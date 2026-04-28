// AppApi/Services/Interfaces/IInboxClassificationService.cs
using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface IInboxClassificationService
{
    Task<ClassifyInboxItemResponseDto> ClassifyInboxItemAsync(
        int inboxItemId,
        ClassifyInboxItemDto request,
        string userId);
}