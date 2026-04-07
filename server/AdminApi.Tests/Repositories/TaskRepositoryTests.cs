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
    public async Task GetAllAsync_ReturnsAllTasks()
    {
        _context.Tasks.AddRange(
            new TaskItem { Title = "A" },
            new TaskItem { Title = "B" }
        );
        await _context.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsTask()
    {
        var task = new TaskItem { Title = "Test" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(task.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(task.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    public void Dispose() => _context.Dispose();
}