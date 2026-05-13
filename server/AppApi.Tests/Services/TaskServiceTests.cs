using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Models;
using FluentAssertions;
using Common.Enums;
using Moq;

namespace AppApi.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repositoryMock;
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly TaskService _service;
    private const string TestUserId = "test-user-123";

    public TaskServiceTests()
    {
        _repositoryMock = new Mock<ITaskRepository>();
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _service = new TaskService(_repositoryMock.Object, _projectRepositoryMock.Object);
    }

    [Fact]
    public async Task GetAllTasksAsync_WithTasks_ReturnsPagedDto()
    {
        var pageNumber = 1;
        var pageSize = 2;

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

        _repositoryMock
            .Setup(r => r.GetAllWithProjectAsync(TestUserId, pageNumber, pageSize))
            .ReturnsAsync((tasks, 5));

        var result = await _service.GetAllTasksAsync(TestUserId, pageNumber, pageSize);

        result.Items.Should().HaveCount(2);
        result.Items.Should().AllBeOfType<TaskResponseDto>();
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);

        _repositoryMock.Verify(r => r.GetAllWithProjectAsync(TestUserId, pageNumber, pageSize), Times.Once);
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
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, TestUserId)).ReturnsAsync(task);

        var result = await _service.GetTaskByIdAsync(1, TestUserId);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Task");
        result.Content.Should().Be("Test Content");
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _repositoryMock.Verify(r => r.GetByIdWithProjectAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetTaskByIdAsync_NonExistingId_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(999, TestUserId)).ReturnsAsync((TaskItem?)null);

        var result = await _service.GetTaskByIdAsync(999, TestUserId);

        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdWithProjectAsync(999, TestUserId), Times.Once);
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
        _repositoryMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        var result = await _service.DeleteTaskAsync(1, TestUserId);

        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_NonExistingTask_ReturnsFalse()
    {
        _repositoryMock.Setup(r => r.SoftDeleteAsync(999, TestUserId)).ReturnsAsync(false);

        var result = await _service.DeleteTaskAsync(999, TestUserId);

        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(999, TestUserId), Times.Once);
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

    [Fact]
    public async Task CreateTaskAsync_WithoutProject_ShouldSucceed()
    {
        var dto = new CreateTaskDto { Title = "No Project Task" };
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => { t.Id = 1; return t; });

        var result = await _service.CreateTaskAsync(dto, TestUserId);

        result.ProjectId.Should().BeNull();
        _projectRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidProjectId_ShouldLinkSuccessfully()
    {
        // Arrange
        var dto = new CreateTaskDto { Title = "Task in Project", ProjectId = 5 };
        var project = new ProjectItem { Id = 5, Name = "My Project", UserId = TestUserId };

        _projectRepositoryMock.Setup(r => r.GetByIdAsync(5, TestUserId, false)).ReturnsAsync(project);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => t);

        // Act
        var result = await _service.CreateTaskAsync(dto, TestUserId);

        // Assert
        result.ProjectId.Should().Be(5);
        result.Project.Name.Should().Be("My Project");
    }

    [Fact]
    public async Task CreateTaskAsync_WithStrangerProjectId_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var dto = new CreateTaskDto { Title = "Hack", ProjectId = 99 };
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(99, TestUserId, false)).ReturnsAsync((ProjectItem?)null);

        // Act & Assert
        await _service.Invoking(s => s.CreateTaskAsync(dto, TestUserId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateTaskAsync_ClearingProject_ShouldSetProjectIdToNull()
    {
        // Arrange
        var dto = new UpdateTaskDto { Title = "No more project", ProjectId = null };
        var updatedTask = new TaskItem { Id = 1, Title = "No more project", ProjectId = null, UserId = TestUserId };

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), TestUserId)).ReturnsAsync(updatedTask);

        // Act
        var result = await _service.UpdateTaskAsync(1, dto, TestUserId);

        // Assert
        result!.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTaskAsync_ChangingProjectToValidOne_ShouldSucceed()
    {
        // Arrange
        var dto = new UpdateTaskDto { Title = "Updated", ProjectId = 10 };
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(10, TestUserId, false))
            .ReturnsAsync(new ProjectItem { Id = 10, UserId = TestUserId, Name = "Test Project" });

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), TestUserId))
            .ReturnsAsync((TaskItem t, string uid) => t);

        // Добавляем мок для GetByIdWithProjectAsync
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, TestUserId))
            .ReturnsAsync(new TaskItem
            {
                Id = 1,
                Title = "Updated",
                ProjectId = 10,
                Project = new ProjectItem { Id = 10, Name = "Test Project" },
                UserId = TestUserId
            });

        // Act
        var result = await _service.UpdateTaskAsync(1, dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.ProjectId.Should().Be(10);
        result.Project.Should().NotBeNull();
        result.Project!.Id.Should().Be(10);
        result.Project.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task CreateTaskAsync_WithValidProject_LinksCorrectly()
    {
        var dto = new CreateTaskDto { Title = "T", ProjectId = 5 };
        // Используем It.IsAny<bool>(), чтобы избежать ошибки несовпадения аргументов
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(5, TestUserId, It.IsAny<bool>()))
            .ReturnsAsync(new ProjectItem { Id = 5, Name = "Pro", UserId = TestUserId });
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<TaskItem>())).ReturnsAsync((TaskItem t) => t);

        var result = await _service.CreateTaskAsync(dto, TestUserId);
        result.ProjectId.Should().Be(5);
        result.Project.Name.Should().Be("Pro");
    }

    [Fact]
    public async Task CreateTaskAsync_WithStrangerProject_ThrowsKeyNotFound()
    {
        var dto = new CreateTaskDto { Title = "T", ProjectId = 99 };
        _projectRepositoryMock.Setup(r => r.GetByIdAsync(99, TestUserId, It.IsAny<bool>())).ReturnsAsync((ProjectItem?)null);

        await _service.Invoking(s => s.CreateTaskAsync(dto, TestUserId)).Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task UpdateTaskAsync_ClearingProject_SetsProjectIdToNull()
    {
        var dto = new UpdateTaskDto { Title = "New", ProjectId = null };
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TaskItem>(), TestUserId)).ReturnsAsync((TaskItem t, string u) => t);
        var result = await _service.UpdateTaskAsync(1, dto, TestUserId);
        result!.ProjectId.Should().BeNull();
    }

    [Fact]
    public async Task SelectTaskForSprintAsync_CompletedTask_ShouldThrow()
    {
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, TestUserId))
            .ReturnsAsync(new TaskItem { Id = 1, UserId = TestUserId, Status = TasksStatus.Completed });

        await _service.Invoking(s => s.SelectTaskForSprintAsync(1, TestUserId))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SelectTaskForSprintAsync_AlreadySelected_ShouldBeIdempotent()
    {
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, TestUserId))
            .ReturnsAsync(new TaskItem { Id = 1, UserId = TestUserId, IsSelectedForSprint = true });

        var result = await _service.SelectTaskForSprintAsync(1, TestUserId);

        result.Should().NotBeNull();
        result!.IsSelectedForSprint.Should().BeTrue();
        _repositoryMock.Verify(r => r.UpdateSprintSelectionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task DeselectTaskForSprintAsync_NotSelected_ShouldBeIdempotent()
    {
        _repositoryMock.Setup(r => r.GetByIdWithProjectAsync(1, TestUserId))
            .ReturnsAsync(new TaskItem { Id = 1, UserId = TestUserId, IsSelectedForSprint = false });

        var result = await _service.DeselectTaskForSprintAsync(1, TestUserId);

        result.Should().NotBeNull();
        result!.IsSelectedForSprint.Should().BeFalse();
        _repositoryMock.Verify(r => r.UpdateSprintSelectionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }
}