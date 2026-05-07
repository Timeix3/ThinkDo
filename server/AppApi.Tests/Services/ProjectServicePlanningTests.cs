// AppApi.Tests/Services/ProjectServicePlanningTests.cs
using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class ProjectServicePlanningTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ITaskRepository> _taskRepositoryMock;
    private readonly ProjectService _service;
    private const string TestUserId = "test-user-123";

    public ProjectServicePlanningTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _taskRepositoryMock = new Mock<ITaskRepository>();
        _service = new ProjectService(_projectRepositoryMock.Object, _taskRepositoryMock.Object);
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_WithProjects_ReturnsCorrectStructure()
    {
        // Arrange
        var projects = new List<ProjectItem>
        {
            new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId },
            new() { Id = 2, Name = "Проект A", UserId = TestUserId }
        };

        var tasksP1 = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task 1", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId }
        };

        var tasksP2 = new List<TaskItem>
        {
            new() { Id = 2, Title = "Task 2", Status = TasksStatus.Available, ProjectId = 2, UserId = TestUserId }
        };

        // Default project exists
        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(projects[0]);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(tasksP1);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(2, TestUserId))
            .ReturnsAsync(tasksP2);

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int> { 2 }); // Task 2 is in sprint

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Projects.Should().HaveCount(2);
        result.TotalProjects.Should().Be(2);

        // Текучка первая
        result.Projects.First().Name.Should().Be("Текучка");
        result.Projects.First().Tasks.Should().HaveCount(1);
        result.Projects.First().Tasks.First().Selected.Should().BeFalse();

        // Проект A второй
        result.Projects.ElementAt(1).Name.Should().Be("Проект A");
        result.Projects.ElementAt(1).Tasks.First().Selected.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_FiltersOnlyAvailableTasks()
    {
        // Arrange
        var projects = new List<ProjectItem>
        {
            new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId }
        };

        var allTasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Available", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId },
            new() { Id = 2, Title = "Completed", Status = TasksStatus.Completed, ProjectId = 1, UserId = TestUserId },
            new() { Id = 3, Title = "InProgress", Status = TasksStatus.InProgress, ProjectId = 1, UserId = TestUserId },
            new() { Id = 4, Title = "Blocked", Status = TasksStatus.Blocked, ProjectId = 1, UserId = TestUserId },
            new() { Id = 5, Title = "Cancelled", Status = TasksStatus.Cancelled, ProjectId = 1, UserId = TestUserId }
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(projects[0]);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(allTasks);

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        var tasks = result.Projects.First().Tasks.ToList();
        tasks.Should().HaveCount(1);
        tasks[0].Title.Should().Be("Available");
        tasks[0].Status.Should().Be(TasksStatus.Available);
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_ProjectWithoutTasks_ReturnsEmptyArray()
    {
        // Arrange (краевой случай: проект без задач)
        var projects = new List<ProjectItem>
        {
            new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId },
            new() { Id = 2, Name = "Пустой проект", UserId = TestUserId }
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(projects[0]);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(new List<TaskItem>());

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(2, TestUserId))
            .ReturnsAsync(new List<TaskItem>());

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        result.Projects.Should().HaveCount(2);
        result.Projects.First().Tasks.Should().BeEmpty();
        result.Projects.ElementAt(1).Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_SortsTekuchkaFirst()
    {
        // Arrange
        var projects = new List<ProjectItem>
        {
            new() { Id = 3, Name = "ZZZ Project", UserId = TestUserId },
            new() { Id = 2, Name = "AAA Project", UserId = TestUserId },
            new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId }
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(projects[2]);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(It.IsAny<int>(), TestUserId))
            .ReturnsAsync(new List<TaskItem>());

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        result.Projects.First().Name.Should().Be("Текучка");
        result.Projects.ElementAt(1).Name.Should().Be("AAA Project");
        result.Projects.ElementAt(2).Name.Should().Be("ZZZ Project");
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_TaskInActiveSprint_MarksAsSelected()
    {
        // Arrange
        var projects = new List<ProjectItem>
        {
            new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId }
        };

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "In Sprint", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId },
            new() { Id = 2, Title = "Not In Sprint", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId }
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(projects[0]);

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(tasks);

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int> { 1 }); // Task 1 in active sprint

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        var returnedTasks = result.Projects.First().Tasks.ToList();
        returnedTasks[0].Id.Should().Be(1);
        returnedTasks[0].Selected.Should().BeTrue();
        returnedTasks[1].Id.Should().Be(2);
        returnedTasks[1].Selected.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_CreatesDefaultProjectIfNotExists()
    {
        // Arrange
        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync((ProjectItem?)null); // Текучка не существует

        _projectRepositoryMock
            .Setup(r => r.AddAsync(It.Is<ProjectItem>(p =>
                p.Name == "Текучка" && p.IsDefault && p.UserId == TestUserId)))
            .ReturnsAsync(new ProjectItem
            {
                Id = 1,
                Name = "Текучка",
                IsDefault = true,
                UserId = TestUserId
            });

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(new List<ProjectItem>
            {
                new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId }
            });

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(new List<TaskItem>());

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        result.Projects.Should().HaveCount(1);
        result.Projects.First().Name.Should().Be("Текучка");

        _projectRepositoryMock.Verify(r => r.AddAsync(It.Is<ProjectItem>(p =>
            p.Name == "Текучка" && p.IsDefault)), Times.Once);
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_RestoresDeletedDefaultProject()
    {
        // Arrange
        var deletedDefault = new ProjectItem
        {
            Id = 1,
            Name = "Текучка",
            IsDefault = true,
            UserId = TestUserId,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(deletedDefault);

        _projectRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<ProjectItem>()))
            .Callback<ProjectItem>(p =>
            {
                p.DeletedAt = null;
                p.UpdatedAt = DateTime.UtcNow;
            });

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(new List<ProjectItem>
            {
                new() { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId }
            });

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(new List<TaskItem>());

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        result.Projects.Should().HaveCount(1);

        _projectRepositoryMock.Verify(r => r.UpdateAsync(It.Is<ProjectItem>(p =>
            p.Id == 1 && p.DeletedAt == null)), Times.Once);
    }

    [Fact]
    public async Task GetPlanningProjectsAsync_OnlyAvailableTasksFiltered()
    {
        // Arrange (фильтрация только available задач)
        var projects = new List<ProjectItem>
        {
            new() { Id = 1, Name = "Проект", UserId = TestUserId }
        };

        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Available 1", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId },
            new() { Id = 2, Title = "Available 2", Status = TasksStatus.Available, ProjectId = 1, UserId = TestUserId },
            new() { Id = 3, Title = "In Progress", Status = TasksStatus.InProgress, ProjectId = 1, UserId = TestUserId },
            new() { Id = 4, Title = "Completed", Status = TasksStatus.Completed, ProjectId = 1, UserId = TestUserId },
            new() { Id = 5, Title = "Cancelled", Status = TasksStatus.Cancelled, ProjectId = 1, UserId = TestUserId },
            new() { Id = 6, Title = "Blocked", Status = TasksStatus.Blocked, ProjectId = 1, UserId = TestUserId }
        };

        _projectRepositoryMock
            .Setup(r => r.GetDefaultProjectAsync(TestUserId, true))
            .ReturnsAsync(new ProjectItem { Id = 1, Name = "Текучка", IsDefault = true, UserId = TestUserId });

        _projectRepositoryMock
            .Setup(r => r.GetAllAsync(TestUserId))
            .ReturnsAsync(projects);

        _taskRepositoryMock
            .Setup(r => r.GetByProjectIdAsync(1, TestUserId))
            .ReturnsAsync(tasks);

        _taskRepositoryMock
            .Setup(r => r.GetActiveSprintTaskIdsAsync(TestUserId))
            .ReturnsAsync(new HashSet<int>());

        // Act
        var result = await _service.GetPlanningProjectsAsync(TestUserId);

        // Assert
        var availableTasks = result.Projects.First().Tasks.ToList();
        availableTasks.Should().HaveCount(2);
        availableTasks.Should().OnlyContain(t => t.Status == TasksStatus.Available);
        availableTasks.Should().Contain(t => t.Title == "Available 1");
        availableTasks.Should().Contain(t => t.Title == "Available 2");
    }
}