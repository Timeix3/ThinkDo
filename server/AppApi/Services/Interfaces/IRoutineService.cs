using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface IRoutineService
{
    Task<IEnumerable<RoutineResponseDto>> GetAllRoutinesAsync(string userId);
    Task<RoutineResponseDto?> GetRoutineByIdAsync(int id, string userId);
    Task<RoutineResponseDto> CreateRoutineAsync(CreateRoutineDto dto, string userId);
    Task<RoutineResponseDto?> UpdateRoutineAsync(int id, UpdateRoutineDto dto, string userId);
    Task<bool> SoftDeleteRoutineAsync(int id, string userId);
}