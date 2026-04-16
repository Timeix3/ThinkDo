using AppApi.Repositories;
using Common.Data;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AppApi.Tests.Integration;

public class TaskRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    private AppDbContext _context = null!;
    private TaskRepository _repository = null!;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
        _repository = new TaskRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
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
    public async Task GetByIdAsync_ExistingTaskAndCorrectUser_ReturnsTask()
    {
        var task = new TaskItem { Title = "Test Task", Content = "Test Content", UserId = TestUserId };
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
        var task = new TaskItem { Title = "Test Task", Content = "Test Content", UserId = OtherUserId };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(task.Id, TestUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidTask_PersistsToDatabase()
    {
        var task = new TaskItem { Title = "New Task", Content = "New Content", UserId = TestUserId };

        var result = await _repository.AddAsync(task);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var inDb = await _context.Tasks.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Title.Should().Be("New Task");
        inDb.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingTask_UpdatesPersisted()
    {
        var task = new TaskItem { Title = "Original", Content = "Original Content", UserId = TestUserId };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updatedTask = new TaskItem { Id = task.Id, Title = "Updated", Content = "Updated Content", UserId = TestUserId };
        var result = await _repository.UpdateAsync(updatedTask, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.UpdatedAt.Should().NotBeNull();

        var inDb = await _context.Tasks.FindAsync(task.Id);
        inDb!.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task DeleteAsync_ExistingTask_RemovesFromDatabase()
    {
        var task = new TaskItem { Title = "Task to Delete", UserId = TestUserId };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.DeleteAsync(task.Id, TestUserId);

        result.Should().BeTrue();
        var inDb = await _context.Tasks.FindAsync(task.Id);
        inDb.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        var now = DateTime.UtcNow;
        _context.Tasks.AddRange(
            new TaskItem { Title = "First", UserId = TestUserId, CreatedAt = now.AddHours(-2) },
            new TaskItem { Title = "Second", UserId = TestUserId, CreatedAt = now.AddHours(-1) },
            new TaskItem { Title = "Third", UserId = TestUserId, CreatedAt = now }
        );
        await _context.SaveChangesAsync();

        var result = (await _repository.GetAllAsync(TestUserId)).ToList();

        result.Should().HaveCount(3);
        result[0].Title.Should().Be("Third");
        result[1].Title.Should().Be("Second");
        result[2].Title.Should().Be("First");
    }
}
