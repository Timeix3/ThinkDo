using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(string userId);
    Task<TaskResponseDto?> GetTaskByIdAsync(int id, string userId);
    Task<TaskResponseDto?> GetTodayTaskAsync(string userId);
    Task<IEnumerable<TaskResponseDto>> GetTodayTasksAsync(string userId);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, string userId);
    Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, string userId);
    Task<bool> DeleteTaskAsync(int id, string userId);
}
