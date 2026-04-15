using AdminApi.Models.DTOs;
using AdminApi.Repositories.Interfaces;
using AdminApi.Services;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AdminApi.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repositoryMock;
    private readonly TaskService _service;
    private const string TestUserId = "test-user-123";

    public TaskServiceTests()
    {
        _repositoryMock = new Mock<ITaskRepository>();
        _service = new TaskService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllTasksAsync_WithTasks_ReturnsAllTasksAsDtos()
    {
        // Arrange
        var tasks = new List<TaskItem>
        {
            new()
            {
                Id = 1,
                Title = "Task 1",
                Content = "Content 1",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow.AddHours(-1)
            },
            new()
            {
                Id = 2,
                Title = "Task 2",
                Content = "Content 2",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow
            }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId)).ReturnsAsync(tasks);

        var result = await _service.GetAllTasksAsync(TestUserId);

        result.Should().HaveCount(2);
        result.Should().AllBeOfType<TaskResponseDto>();
        _repositoryMock.Verify(r => r.GetAllAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetTaskByIdAsync_ExistingId_ReturnsDto()
    {
        var task = new TaskItem
        {
            Id = 1,
            Title = "Test Task",
            Content = "Test Content",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(task);

        var result = await _service.GetTaskByIdAsync(1, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.Content.Should().Be("Test Content");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _repositoryMock.Verify(r => r.GetByIdAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistingId_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999, TestUserId)).ReturnsAsync((TaskItem?)null);

        var result = await _service.GetTaskByIdAsync(999, TestUserId);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(999, TestUserId), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ValidDto_CallsRepositoryAndReturnsDto()
    {
        var dto = new CreateTaskDto { Title = "New Task", Content = "New Content" };
        var createdTask = new TaskItem
        {
            Id = 1,
            Title = "New Task",
            Content = "New Content",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.AddAsync(It.Is<TaskItem>(t =>
                t.Title == "New Task" &&
                t.Content == "New Content" &&
                t.UserId == TestUserId)))
            .ReturnsAsync(createdTask);

        var result = await _service.CreateTaskAsync(dto, TestUserId);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");
        result.Content.Should().Be("New Content");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _repositoryMock.Verify(r => r.AddAsync(It.Is<TaskItem>(t =>
            t.Title == "New Task" &&
            t.UserId == TestUserId)), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_ExistingTask_ReturnsUpdatedDto()
    {
        var dto = new UpdateTaskDto { Title = "Updated", Content = "Updated Content" };
        var updatedTask = new TaskItem
        {
            Id = 1,
            Title = "Updated",
            Content = "Updated Content",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), TestUserId))
            .ReturnsAsync(updatedTask);

        var result = await _service.UpdateTaskAsync(1, dto, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        result.Content.Should().Be("Updated Content");
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<TaskItem>(t => t.Id == 1 && t.UserId == TestUserId),
            TestUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_NonExistingTask_ReturnsNull()
    {
        var dto = new UpdateTaskDto { Title = "Updated", Content = "Updated Content" };
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), TestUserId))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.UpdateTaskAsync(999, dto, TestUserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_ReturnsTrue()
    {
        _repositoryMock.Setup(r => r.DeleteAsync(1, TestUserId)).ReturnsAsync(true);

        var result = await _service.DeleteTaskAsync(1, TestUserId);

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistingTask_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.DeleteAsync(999, TestUserId)).ReturnsAsync(false);

        var result = await _service.DeleteTaskAsync(999, TestUserId);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.DeleteAsync(999, TestUserId), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_SetsUserIdCorrectly()
    {
        var dto = new CreateTaskDto { Title = "New Task", Content = "New Content" };
        TaskItem? capturedTask = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => capturedTask = t)
            .ReturnsAsync((TaskItem t) => t);

        await _service.CreateTaskAsync(dto, TestUserId);

        capturedTask.Should().NotBeNull();
        capturedTask!.UserId.Should().Be(TestUserId);
    }
}