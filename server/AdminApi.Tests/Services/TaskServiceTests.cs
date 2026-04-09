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

    public TaskServiceTests()
    {
        _repositoryMock = new Mock<ITaskRepository>();
        _service = new TaskService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllTasksAsync_WithTasks_ReturnsAllTasksAsDtos()
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Content = "Content 1" },
            new() { Id = 2, Title = "Task 2", Content = "Content 2" }
        };
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

        var result = await _service.GetAllTasksAsync();

        result.Should().HaveCount(2);
        result.Should().AllBeOfType<TaskResponseDto>();
    }

    [Fact]
    public async Task GetTaskByIdAsync_ExistingId_ReturnsDto()
    {
        var task = new TaskItem { Id = 1, Title = "Test Task", Content = "Test Content" };
        _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(task);

        var result = await _service.GetTaskByIdAsync(1);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.Content.Should().Be("Test Content");
    }

    [Fact]
    public async Task CreateTaskAsync_ValidDto_CallsRepositoryAndReturnsDto()
    {
        var dto = new CreateTaskDto { Title = "New Task", Content = "New Content" };
        var createdTask = new TaskItem { Id = 1, Title = "New Task", Content = "New Content" };

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync(createdTask);

        var result = await _service.CreateTaskAsync(dto);

        result.Should().NotBeNull();
        result.Title.Should().Be("New Task");
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_ExistingTask_ReturnsUpdatedDto()
    {
        var dto = new UpdateTaskDto { Title = "Updated", Content = "Updated Content" };
        var updatedTask = new TaskItem { Id = 1, Title = "Updated", Content = "Updated Content" };

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync(updatedTask);

        var result = await _service.UpdateTaskAsync(1, dto);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated");
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TaskItem>()), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_ExistingTask_ReturnsTrue()
    {
        _repositoryMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _service.DeleteTaskAsync(1);

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
    }
}