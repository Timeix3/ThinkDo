using AdminApi.Models.DTOs;
using AdminApi.Repositories.Interfaces;
using AdminApi.Services.Interfaces;
using Common.Data;
using Common.Models;

namespace AdminApi.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly AppDbContext _context;

    public TaskService(ITaskRepository taskRepository, AppDbContext context)
    {
        _taskRepository = taskRepository;
        _context = context;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync()
    {
        var tasks = await _taskRepository.GetAllAsync();
        return tasks.Select(MapToDto);
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        return task is null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateTaskAsync(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Content = dto.Content,
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(int id, UpdateTaskDto dto)
    {
        // Читаем через репозиторий
        var task = await _taskRepository.GetByIdAsync(id);

        if (task is null)
            return null;

        // Мутация — в сервисе
        task.Title = dto.Title;
        task.Content = dto.Content;

        await _context.SaveChangesAsync();

        return MapToDto(task);
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id);

        if (task is null)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    private static TaskResponseDto MapToDto(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Content = task.Content,
    };
}