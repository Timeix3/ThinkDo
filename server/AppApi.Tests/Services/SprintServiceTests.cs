using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Models;
using Common.Enums;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class SprintServiceTests
{
    private readonly Mock<ISprintRepository> _sprintRepoMock;
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly Mock<IInboxRepository> _inboxRepoMock;
    private readonly SprintService _service;
    private const string UserId = "user-123";

    public SprintServiceTests()
    {
        _sprintRepoMock = new Mock<ISprintRepository>();
        _taskRepoMock = new Mock<ITaskRepository>();
        _inboxRepoMock = new Mock<IInboxRepository>();
        _service = new SprintService(_sprintRepoMock.Object, _taskRepoMock.Object, _inboxRepoMock.Object);
    }

    // --- ТЕСТЫ ОПРЕДЕЛЕНИЯ ФАЗ  ---

    [Fact]
    public async Task GetStatus_ActiveSprintWithTasks_ReturnsSprintPhase()
    {
        // Arrange: Есть активный спринт с одной невыполненной задачей
        var activeSprint = new SprintItem { 
            UserId = UserId, 
            Status = SprintStatus.Active,
            Tasks = new List<TaskItem> { new() { Status = TasksStatus.Available } } 
        };
        _sprintRepoMock.Setup(r => r.GetActiveSprintAsync(UserId)).ReturnsAsync(activeSprint);

        // Act
        var result = await _service.GetStatusAsync(UserId);

        // Assert
        result.Phase.Should().Be("sprint");
        result.HasActiveSprint.Should().BeTrue();
    }

    [Fact]
    public async Task GetStatus_NoSprintButInboxNotEmpty_ReturnsReviewPhase()
    {
        // Arrange: Спринта нет, но в инбоксе 5 записей
        _sprintRepoMock.Setup(r => r.GetActiveSprintAsync(UserId)).ReturnsAsync((SprintItem?)null);
        _inboxRepoMock.Setup(r => r.GetCountAsync(UserId)).ReturnsAsync(5);

        // Act
        var result = await _service.GetStatusAsync(UserId);

        // Assert
        result.Phase.Should().Be("review");
        result.InboxCount.Should().Be(5);
    }

    [Fact]
    public async Task GetStatus_NoSprintEmptyInbox_ReturnsPlanningPhase()
    {
        // Arrange: Чистая база
        _sprintRepoMock.Setup(r => r.GetActiveSprintAsync(UserId)).ReturnsAsync((SprintItem?)null);
        _inboxRepoMock.Setup(r => r.GetCountAsync(UserId)).ReturnsAsync(0);

        // Act
        var result = await _service.GetStatusAsync(UserId);

        // Assert
        result.Phase.Should().Be("planning");
    }

    // --- ТЕСТЫ СТАРТА СПРИНТА ---

    [Fact]
    public async Task StartSprint_WhenAlreadyActive_ThrowsException()
    {
        // Arrange: Спринт уже есть
        _sprintRepoMock.Setup(r => r.GetActiveSprintAsync(UserId)).ReturnsAsync(new SprintItem());

        // Act & Assert
        await _service.Invoking(s => s.StartSprintAsync(new List<int>{1}, UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Спринт уже запущен.");
    }

    [Fact]
    public async Task StartSprint_WithTasks_SavesSuccessfully()
    {
        // Arrange
        var taskIds = new List<int> { 1, 2 };
        var tasks = new List<TaskItem> { 
            new() { Id = 1, UserId = UserId }, 
            new() { Id = 2, UserId = UserId } 
        };
        _sprintRepoMock.Setup(r => r.GetActiveSprintAsync(UserId)).ReturnsAsync((SprintItem?)null);
        _taskRepoMock.Setup(r => r.GetByIdsAsync(taskIds, UserId)).ReturnsAsync(tasks);

        // Act
        var result = await _service.StartSprintAsync(taskIds, UserId);

        // Assert
        result.Success.Should().BeTrue();
        result.TasksCount.Should().Be(2);
        _sprintRepoMock.Verify(r => r.AddAsync(It.Is<SprintItem>(s => s.Tasks.Count == 2)), Times.Once);
    }
}