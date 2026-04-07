using AdminApi.Models.DTOs;
using AdminApi.Repositories.Interfaces;
using AdminApi.Services;
using AdminApi.Tests.Helpers;
using Common.Data;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AdminApi.Tests.Services;

public class TaskServiceTests : IDisposable
{
    private readonly Mock<ITaskRepository> _repoMock;
    private readonly AppDbContext _context;
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _repoMock = new Mock<ITaskRepository>();
        _context = DbContextHelper.CreateInMemoryContext();
        _service = new TaskService(_repoMock.Object, _context);
    }

    [Fact]
    public async Task GetAllTasksAsync_ReturnsAllTasksAsDtos()
    {
        _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync([
            new TaskItem { Id = 1, Title = "A" },
            new TaskItem { Id = 2, Title = "B" }
        ]);

        var result = await _service.GetAllTasksAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ExistingId_ReturnsDto()
    {
        var task = new TaskItem { Id = 1, Title = "Test" };
        _repoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(task);

        var result = await _service.GetTaskByIdAsync(1);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

        var result = await _service.GetTaskByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesAndReturnsDto()
    {
        var dto = new CreateTaskDto { Title = "New", Content = "Content" };

        var result = await _service.CreateTaskAsync(dto);

        result.Title.Should().Be("New");
        _context.Tasks.Should().ContainSingle(t => t.Title == "New");
    }

    [Fact]
    public async Task UpdateTaskAsync_ExistingId_ReturnsUpdatedDto()
    {
        var task = new TaskItem { Title = "Old" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        _repoMock.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var result = await _service.UpdateTaskAsync(task.Id, new UpdateTaskDto { Title = "New" });

        result.Should().NotBeNull();
        result!.Title.Should().Be("New");
    }

    [Fact]
    public async Task UpdateTaskAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

        var result = await _service.UpdateTaskAsync(999, new UpdateTaskDto { Title = "X" });

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingId_ReturnsTrue()
    {
        var task = new TaskItem { Title = "To delete" };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();
        _repoMock.Setup(r => r.GetByIdAsync(task.Id)).ReturnsAsync(task);

        var result = await _service.DeleteTaskAsync(task.Id);

        result.Should().BeTrue();
        _context.Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((TaskItem?)null);

        var result = await _service.DeleteTaskAsync(999);

        result.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}