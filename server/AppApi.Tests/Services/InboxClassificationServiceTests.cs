using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
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

    private ClassifyInboxItemDto CreateClassifyRequest(string entityType, object data, string mode = "convert")
    {
        var json = JsonSerializer.Serialize(new
        {
            entityType,
            mode,
            entityData = data
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

    // --- ТЕСТЫ КОНВЕРТАЦИИ (mode: convert) ---

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidTask_ModeConvert_CreatesTaskAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("task", new { title = "New Task", content = "Task content" }, "convert");

        var createdTask = new TaskItem { Id = 42, Title = "New Task", UserId = TestUserId };

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync(createdTask);
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.Success.Should().BeTrue();
        result.CreatedEntityId.Should().Be(42);
        result.InboxDeleted.Should().BeTrue();

        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidProject_CreatesProjectAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("project", new { title = "New Project" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);
        _projectRepoMock.Setup(r => r.AddAsync(It.IsAny<ProjectItem>())).ReturnsAsync(new ProjectItem { Id = 10 });
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.CreatedEntityId.Should().Be(10);
        result.InboxDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidRoutine_CreatesRoutineAndDeletesInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("routine", new { title = "Run", frequency = "Daily" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);
        _routineRepoMock.Setup(r => r.AddAsync(It.IsAny<Routine>())).ReturnsAsync(new Routine { Id = 5 });
        _inboxRepoMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.CreatedEntityId.Should().Be(5);
        result.InboxDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_ValidTask_ModeCreate_KeepsInboxItem()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("task", new { title = "Keep Me" }, "create");

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync(new TaskItem { Id = 77 });

        // Act
        var result = await _service.ClassifyInboxItemAsync(1, request, TestUserId);

        // Assert
        result.InboxDeleted.Should().BeFalse();
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    // --- ТЕСТЫ ОШИБОК И КРАЕВЫХ СЛУЧАЕВ ---

    [Fact]
    public async Task ClassifyInboxItemAsync_InvalidMode_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateClassifyRequest("task", new { title = "Test" }, "invalid_mode");
        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(CreateInboxItem());

        // Act & Assert
        await _service.Invoking(s => s.ClassifyInboxItemAsync(1, request, TestUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid mode*");
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_NonExistentInbox_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = CreateClassifyRequest("task", new { title = "Test" });
        _inboxRepoMock.Setup(r => r.GetByIdAsync(99, TestUserId)).ReturnsAsync((InboxItem?)null);

        // Act & Assert
        await _service.Invoking(s => s.ClassifyInboxItemAsync(99, request, TestUserId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_InvalidEntityType_ThrowsArgumentException()
    {
        // Arrange
        var request = CreateClassifyRequest("wrong_type", new { title = "Test" });
        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(CreateInboxItem());

        // Act & Assert
        await _service.Invoking(s => s.ClassifyInboxItemAsync(1, request, TestUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Unsupported entity type*");
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_TaskCreationFails_DoesNotDeleteInbox()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("task", new { title = "Safe Item" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);
        _taskRepoMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ThrowsAsync(new Exception("Database error"));

        // Act
        await _service.Invoking(s => s.ClassifyInboxItemAsync(1, request, TestUserId))
            .Should().ThrowAsync<Exception>();

        // Assert: Мягкое удаление не должно вызываться
        _inboxRepoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_RoutineMissingFrequency_ThrowsArgumentException()
    {
        // Arrange
        var inboxItem = CreateInboxItem();
        var request = CreateClassifyRequest("routine", new { title = "Test" }); // frequency отсутствует

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(inboxItem);

        // Act & Assert
        await _service.Invoking(s => s.ClassifyInboxItemAsync(1, request, TestUserId))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Frequency is required*");
    }

    [Fact]
    public async Task ClassifyInboxItemAsync_AlreadyDeletedInbox_ThrowsKeyNotFoundException()
    {
        // Arrange
        var deletedInbox = CreateInboxItem(1, true); // DeletedAt != null
        var request = CreateClassifyRequest("task", new { title = "Test" });

        _inboxRepoMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(deletedInbox);

        // Act & Assert
        await _service.Invoking(s => s.ClassifyInboxItemAsync(1, request, TestUserId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }
}