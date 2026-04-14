using AdminApi.Repositories;
using AdminApi.Tests.Helpers;
using Common.Models;
using FluentAssertions;

namespace AdminApi.Tests.Repositories;

public class TaskRepositoryTests : IDisposable
{
    private readonly Common.Data.AppDbContext _context;
    private readonly TaskRepository _repository;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public TaskRepositoryTests()
    {
        _context = DbContextHelper.CreateInMemoryContext();
        _repository = new TaskRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_WithUserTasks_ReturnsOnlyUserTasks()
    {
        _context.Tasks.AddRange(
            new TaskItem { Title = "Task 1", Content = "Content 1", UserId = TestUserId },
            new TaskItem { Title = "Task 2", Content = "Content 2", UserId = TestUserId },
            new TaskItem { Title = "Task 3", Content = "Content 3", UserId = OtherUserId }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(TestUserId);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.UserId == TestUserId);
    }

    [Fact]
    public async Task GetAllAsync_NoTasksForUser_ReturnsEmptyList()
    {
        _context.Tasks.Add(new TaskItem 
        { 
            Title = "Other User Task", 
            Content = "Content", 
            UserId = OtherUserId 
        });
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync(TestUserId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTaskAndCorrectUser_ReturnsTask()
    {
        var task = new TaskItem 
        { 
            Title = "Test Task", 
            Content = "Test Content", 
            UserId = TestUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(task.Id, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingTaskButWrongUser_ReturnsNull()
    {
        var task = new TaskItem 
        { 
            Title = "Test Task", 
            Content = "Test Content", 
            UserId = OtherUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(task.Id, TestUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidTask_AddsTaskToDatabase()
    {
        var task = new TaskItem 
        { 
            Title = "New Task", 
            Content = "New Content", 
            UserId = TestUserId 
        };

        var result = await _repository.AddAsync(task);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");
        result.UserId.Should().Be(TestUserId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var inDb = await _context.Tasks.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTaskAndCorrectUser_UpdatesTask()
    {
        var task = new TaskItem 
        { 
            Title = "Original", 
            Content = "Original Content", 
            UserId = TestUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updatedTask = new TaskItem
        {
            Id = task.Id,
            Title = "Updated",
            Content = "Updated Content",
            UserId = TestUserId
        };

        var result = await _repository.UpdateAsync(updatedTask, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.Content.Should().Be("Updated Content");
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_ExistingTaskButWrongUser_ReturnsNull()
    {
        var task = new TaskItem 
        { 
            Title = "Original", 
            Content = "Original Content", 
            UserId = OtherUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var updatedTask = new TaskItem
        {
            Id = task.Id,
            Title = "Updated",
            Content = "Updated Content",
            UserId = TestUserId
        };

        var result = await _repository.UpdateAsync(updatedTask, TestUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingTaskAndCorrectUser_DeletesTaskAndReturnsTrue()
    {
        var task = new TaskItem 
        { 
            Title = "Task to Delete", 
            UserId = TestUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(task.Id, TestUserId);

        result.Should().BeTrue();

        var inDb = await _context.Tasks.FindAsync(task.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ExistingTaskButWrongUser_ReturnsFalse()
    {
        var task = new TaskItem 
        { 
            Title = "Task to Delete", 
            UserId = OtherUserId 
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(task.Id, TestUserId);

        result.Should().BeFalse();

        var inDb = await _context.Tasks.FindAsync(task.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        var now = DateTime.UtcNow;
        _context.Tasks.AddRange(
            new TaskItem 
            { 
                Title = "First", 
                UserId = TestUserId, 
                CreatedAt = now.AddHours(-2) 
            },
            new TaskItem 
            { 
                Title = "Second", 
                UserId = TestUserId, 
                CreatedAt = now.AddHours(-1) 
            },
            new TaskItem 
            { 
                Title = "Third", 
                UserId = TestUserId, 
                CreatedAt = now 
            }
        );
        await _context.SaveChangesAsync();

        var result = (await _repository.GetAllAsync(TestUserId)).ToList();

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Third");
        result[1].Title.Should().Be("Second");
        result[2].Title.Should().Be("First");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}