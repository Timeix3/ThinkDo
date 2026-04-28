// AppApi.Tests/Services/InboxClassificationServiceTests.cs
using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;
using System.Text.Json;

namespace AppApi.Tests.Services;

public class InboxClassificationServiceTests
{
    private readonly Mock<IInboxRepository> _inboxRepoMock;
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly Mock<IRoutineRepository> _routineRepoMock;
    private readonly Mock<IProjectRepository> _projectRepoMock;
    private readonly Mock<ILogger<InboxClassificationService>> _loggerMock;
    private readonly InboxClassificationService _service;
    private const string TestUserId = "test-user-123";

    public InboxClassificationServiceTests()
    {
        _inboxRepoMock = new Mock<IInboxRepository>();
        _taskRepoMock = new Mock<ITaskRepository>();
        _routineRepoMock = new Mock<IRoutineRepository>();
        _projectRepoMock = new Mock<IProjectRepository>();
        _loggerMock = new Mock<ILogger<InboxClassificationService>>();

        _service = new InboxClassificationService(
            _inboxRepoMock.Object,
            _taskRepoMock.Object,
            _routineRepoMock.Object,
            _projectRepoMock.Object,
            _loggerMock.Object);
    }

    private ClassifyInboxItemDto CreateClassifyRequest(string targetType, object data)
    {
        var json = JsonSerializer.Serialize(new
        {
            targetType,
            data
        });

        return JsonSerializer.Deserialize<ClassifyInboxItemDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    private InboxItem CreateInboxItem(int id = 1, bool deleted = false)
    {
        return new InboxItem
        {
            Id = id,
            Title = "Test Inbox Item",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = deleted ? DateTime.UtcNow : null
        };
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidTask_CreatesTaskAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("task", new { title = "New Task", content = "Task content" });

        var createdTask = new TaskItem
        {
            Id = 1,
            Title = "New Task",
            Content = "Task content",
            UserId = TestUserId,
            Status = TasksStatus.Available,
            CreatedAt = DateTime.UtcNow
        };

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .ReturnsAsync(createdTask);
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.TargetType.Should().Be("task");
        result.Title.Should().Be("New Task");
        result.Id.Should().Be(1);
        result.Status.Should().Be("Available");

        _inboxRepoMock.Verify(r => r.GetByIdAsync(1, TestUserId), Times.Once);
        _taskRepoMock.Verify(r => r.AddAsync(It.Is<TaskItem>(t =>
            t.Title == "New Task" && t.UserId == TestUserId)), Times.Once);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidProject_CreatesProjectAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("project", new
        {
            title = "New Project",
            description = "Project description"
        });

        var createdProject = new ProjectItem
        {
            Id = 1,
            Name = "New Project",
            Description = "Project description",
            UserId = TestUserId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);
        _projectRepoMock!.Setup(r => r.AddAsync(It.IsAny<ProjectItem>()))
            .ReturnsAsync(createdProject);
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.TargetType.Should().Be("project");
        result.Title.Should().Be("New Project");
        result.Description.Should().Be("Project description");

        _inboxRepoMock.Verify(r => r.GetByIdAsync(1, TestUserId), Times.Once);
        _projectRepoMock.Verify(r => r.AddAsync(It.Is<ProjectItem>(p =>
            p.Name == "New Project" && p.UserId == TestUserId)), Times.Once);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidRoutine_CreatesRoutineAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("routine", new
        {
            title = "Morning Exercise",
            frequency = "daily"
        });

        var createdRoutine = new Routine
        {
            Id = 1,
            Name = "Morning Exercise",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);
        _routineRepoMock.Setup(r => r.AddAsync(It.IsAny<Routine>()))
            .ReturnsAsync(createdRoutine);
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.TargetType.Should().Be("routine");
        result.Title.Should().Be("Morning Exercise");
        result.Frequency.Should().Be("Daily");

        _inboxRepoMock.Verify(r => r.GetByIdAsync(1, TestUserId), Times.Once);
        _routineRepoMock.Verify(r => r.AddAsync(It.Is<Routine>(r =>
            r.Name == "Morning Exercise" && r.Frequency == RoutineFrequency.Daily)), Times.Once);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_NonExistentInbox_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = CreateClassifyRequest("task", new { title = "Test" });
        _inboxRepoMock.Setup(r => r.GetByIdAsync(999, TestUserId))
            .ReturnsAsync((InboxItem?)null);

        // Act
        Func<Task> act = () => _service.ClassifyInboxItemAsync(999, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*not found*");

        _taskRepoMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_AlreadyDeletedInbox_ThrowsInvalidOperationException()
    {
        // Arrange
        var deletedInbox = CreateInboxItem(1, true);
        var request = CreateClassifyRequest("task", new { title = "Test" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(deletedInbox);

        // Act
        Func<Task> act = () => _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been classified*");

        _taskRepoMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_InvalidTargetType_ThrowsArgumentException()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("invalid_type", new { title = "Test" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);

        // Act
        Func<Task> act = () => _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid targetType*");

        _taskRepoMock.Verify(r => r.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_TaskCreationFails_DoesNotDeleteInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("task", new { title = "Test" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        Func<Task> act = () => _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_RoutineMissingFrequency_ThrowsArgumentException()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("routine", new { title = "Test" }); // No frequency

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);

        // Act
        Func<Task> act = () => _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Frequency is required*");

        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_UsesInboxTitleAsContent_WhenContentNotProvided()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        inboxItem.Title = "Original inbox title";
        var request = CreateClassifyRequest("task", new { title = "Task from inbox" }); // No content

        TaskItem? capturedTask = null;

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId))
            .ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>()))
            .Callback<TaskItem>(t => capturedTask = t)
            .ReturnsAsync((TaskItem t) => { t.Id = 1; return t; });
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        capturedTask.Should().NotBeNull();
        capturedTask!.Content.Should().Be("Original inbox title");
    }
}