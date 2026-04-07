using AdminApi.Models.DTOs;

namespace AdminApi.Services.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync();
    Task<TaskResponseDto?> GetTaskByIdAsync(int id);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto);
    Task<bool> DeleteTaskAsync(int id);
}