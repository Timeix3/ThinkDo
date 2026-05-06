using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface ISprintService
{
    Task<SprintStatusDto> GetStatusAsync(string userId);
    Task<StartSprintResponseDto> StartSprintAsync(List<int> taskIds, string userId);
    Task<CompleteSprintResponseDto> CompleteSprintAsync(string userId);
}