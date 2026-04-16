using AppApi.Models.DTOs;

namespace AppApi.Services.Interfaces;

public interface ITaskService
{
    Task<TaskListResponseDto> GetAllTasksAsync(string userId, int offset = 0, int limit = 50);
    Task<TaskResponseDto?> GetTaskByIdAsync(int id, string userId);
    Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto, string userId);
    Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto, string userId);
    Task<bool> DeleteTaskAsync(int id, string userId);
    Task<TaskResponseDto?> CompleteTaskAsync(int id, string userId);
    Task<TaskResponseDto?> CancelTaskAsync(int id, string userId);
}