using AdminApi.Repositories;
using AdminApi.Tests.Helpers;
using Common.Models;
using FluentAssertions;

namespace AdminApi.Tests.Repositories;

public class TaskRepositoryTests : IDisposable
{
    private readonly Common.Data.AppDbContext _context;
    private readonly TaskRepository _repository;

    public TaskRepositoryTests()
    {
        _context = DbContextHelper.CreateInMemoryContext();
        _repository = new TaskRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_WithTasks_ReturnsAllTasks()
    {
        _context.Tasks.AddRange(
            new TaskItem { Title = "Task 1", Content = "Content 1" },
            new TaskItem { Title = "Task 2", Content = "Content 2" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        var task = new TaskItem { Title = "Test Task", Content = "Test Content" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
    }

    [Fact]
    public async Task AddAsync_ValidTask_AddsTaskToDatabase()
    {
        var task = new TaskItem { Title = "New Task", Content = "New Content" };

        var result = await _repository.AddAsync(task);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");

        var inDb = await _context.Tasks.FindAsync(result.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesTaskInDatabase()
    {
        var task = new TaskItem { Title = "Original", Content = "Original Content" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.UpdateAsync(new TaskItem
        {
            Id = task.Id,
            Title = "Updated",
            Content = "Updated Content"
        });

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.Content.Should().Be("Updated Content");
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_DeletesTaskAndReturnsTrue()
    {
        var task = new TaskItem { Title = "Task to Delete" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(task.Id);

        result.Should().BeTrue();

        var inDb = await _context.Tasks.FindAsync(task.Id);
        inDb.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}