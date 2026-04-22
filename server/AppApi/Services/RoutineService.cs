using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services.Interfaces;
using Common.Models;
using Common.Enums;

namespace AppApi.Services;

public class RoutineService : IRoutineService
{
    private readonly IRoutineRepository _repository;

    public RoutineService(IRoutineRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<RoutineResponseDto>> GetAllRoutinesAsync(string userId)
    {
        var routines = await _repository.GetAllAsync(userId);
        return routines.Select(MapToDto);
    }

    public async Task<RoutineResponseDto?> GetRoutineByIdAsync(int id, string userId)
    {
        var routine = await _repository.GetByIdAsync(id, userId);
        return routine is null ? null : MapToDto(routine);
    }

    public async Task<RoutineResponseDto> CreateRoutineAsync(CreateRoutineDto dto, string userId)
    {
        // Валидация пустого имени
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Name cannot be empty or whitespace");
        }

        var routine = new Routine
        {
            Name = dto.Name.Trim(),
            Frequency = dto.Frequency,
            UserId = userId
        };

        var created = await _repository.AddAsync(routine);
        return MapToDto(created);
    }

    public async Task<RoutineResponseDto?> UpdateRoutineAsync(int id, UpdateRoutineDto dto, string userId)
    {
        // Валидация пустого имени
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ArgumentException("Name cannot be empty or whitespace");
        }

        var routine = new Routine
        {
            Id = id,
            Name = dto.Name.Trim(),
            Frequency = dto.Frequency,
            UserId = userId
        };

        var updated = await _repository.UpdateAsync(routine, userId);
        return updated is null ? null : MapToDto(updated);
    }

    public async Task<bool> SoftDeleteRoutineAsync(int id, string userId)
    {
        return await _repository.SoftDeleteAsync(id, userId);
    }

    private static RoutineResponseDto MapToDto(Routine routine) => new()
    {
        Id = routine.Id,
        Name = routine.Name,
        Frequency = routine.Frequency,
        CreatedAt = routine.CreatedAt,
        UpdatedAt = routine.UpdatedAt
    };
}