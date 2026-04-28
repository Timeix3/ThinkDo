using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _repoMock;
    private readonly Mock<ITaskRepository> _taskRepoMock;
    private readonly ProjectService _service;
    private const string UserId = "user-123";

    public ProjectServiceTests()
    {
        _repoMock = new Mock<IProjectRepository>();
        _taskRepoMock = new Mock<ITaskRepository>();
        _service = new ProjectService(_repoMock.Object, _taskRepoMock.Object);
    }

    [Fact]
    public async Task CreateProject_ShouldTrimName()
    {
        // Edge Case: Пользователь ввел имя с кучей пробелов
        var dto = new CreateProjectDto { Name = "  My Project  " };
        _repoMock.Setup(r => r.AddAsync(It.IsAny<ProjectItem>()))
                 .ReturnsAsync((ProjectItem p) => p);

        var result = await _service.CreateProjectAsync(dto, UserId);

        result.Name.Should().Be("My Project");
    }

    [Fact]
    public async Task UpdateProject_SystemProject_ChangeName_ThrowsException()
    {
        // Краевой случай: Попытка переименовать Текучку
        var systemProject = new ProjectItem { Id = 1, Name = "Текучка", IsDefault = true, UserId = UserId };
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, false)).ReturnsAsync(systemProject);

        var updateDto = new UpdateProjectDto { Name = "Взломанное Имя" };

        await _service.Invoking(s => s.UpdateProjectAsync(1, updateDto, UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*'Текучка' нельзя изменить*");
    }

    [Fact]
    public async Task UpdateProject_SystemProject_ChangeDescription_Success()
    {
        // Краевой случай: Описание Текучки менять МОЖНО, а имя - НЕТ
        var systemProject = new ProjectItem { Id = 1, Name = "Текучка", IsDefault = true, UserId = UserId };
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, false)).ReturnsAsync(systemProject);
        var updateDto = new UpdateProjectDto { Name = "Текучка", Description = "Новое описание" };

        var result = await _service.UpdateProjectAsync(1, updateDto, UserId);

        result.Should().NotBeNull();
        result!.Description.Should().Be("Новое описание");
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<ProjectItem>()), Times.Once);
    }

    [Fact]
    public async Task DeleteProject_SystemProject_ThrowsException()
    {
        // Критическое правило: Текучку нельзя удалять
        var systemProject = new ProjectItem { Id = 1, IsDefault = true, UserId = UserId };
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, false)).ReturnsAsync(systemProject);

        await _service.Invoking(s => s.DeleteProjectAsync(1, UserId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*не может быть удален*");
    }

    [Fact]
    public async Task GetProjectById_WrongUser_ReturnsNull()
    {
        // Изоляция: Юзер А пытается получить проект Юзера Б
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, true)).ReturnsAsync((ProjectItem?)null);

        var result = await _service.GetProjectByIdAsync(1, UserId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProjects_WhenDefaultProjectMissing_ShouldCreateNewDefault()
    {
        // Arrange: У пользователя вообще нет проектов
        _repoMock.Setup(r => r.GetAllAsync(UserId)).ReturnsAsync(new List<ProjectItem>());
        _repoMock.Setup(r => r.GetDefaultProjectAsync(UserId, true)).ReturnsAsync((ProjectItem?)null);

        _repoMock.Setup(r => r.AddAsync(It.IsAny<ProjectItem>()))
                 .ReturnsAsync((ProjectItem p) =>
                 {
                     p.Id = 1;
                     return p;
                 })
                 .Callback<ProjectItem>(p =>
                 {
                     // Имитируем, что после добавления проект появится в списке
                     _repoMock.Setup(r => r.GetAllAsync(UserId))
                              .ReturnsAsync(new List<ProjectItem> { p });
                 });

        // Act
        var result = await _service.GetProjectsAsync(UserId);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Текучка");
        result.First().IsDefault.Should().BeTrue();

        _repoMock.Verify(r => r.AddAsync(It.Is<ProjectItem>(p => p.IsDefault && p.Name == "Текучка")), Times.Once);
    }

    [Fact]
    public async Task GetProjects_WhenDefaultProjectDeleted_ShouldRestoreIt()
    {
        // Arrange: Текучка удалена (DeletedAt != null)
        var deletedDefault = new ProjectItem
        {
            Id = 1,
            Name = "Текучка",
            IsDefault = true,
            UserId = UserId,
            DeletedAt = DateTime.UtcNow
        };

        _repoMock.Setup(r => r.GetAllAsync(UserId)).ReturnsAsync(new List<ProjectItem>());
        _repoMock.Setup(r => r.GetDefaultProjectAsync(UserId, true)).ReturnsAsync(deletedDefault);

        // Act
        await _service.GetProjectsAsync(UserId);

        // Assert
        // Проверяем, что поле DeletedAt было занулено
        deletedDefault.DeletedAt.Should().BeNull();
        // Проверяем, что репозиторий вызвал Update
        _repoMock.Verify(r => r.UpdateAsync(It.Is<ProjectItem>(p => p.Id == 1 && p.DeletedAt == null)), Times.Once);
    }

    [Fact]
    public async Task UpdateProject_NormalProject_ShouldWorkCorrectly()
    {
        // Arrange: Обычный проект (не системный)
        var normalProject = new ProjectItem { Id = 10, Name = "Старое имя", IsDefault = false, UserId = UserId };
        _repoMock.Setup(r => r.GetByIdAsync(10, UserId, false)).ReturnsAsync(normalProject);

        var updateDto = new UpdateProjectDto { Name = "Новое имя", Description = "Новое описание" };

        // Act
        var result = await _service.UpdateProjectAsync(10, updateDto, UserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Новое имя");
        _repoMock.Verify(r => r.UpdateAsync(It.Is<ProjectItem>(p => p.Name == "Новое имя")), Times.Once);
    }

    [Fact]
    public async Task GetProjectTasksAsync_StrangerProject_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByIdAsync(99, UserId, true)).ReturnsAsync((ProjectItem?)null);

        // Act & Assert
        await _service.Invoking(s => s.GetProjectTasksAsync(99, UserId))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetProjectTasksAsync_ValidProject_ReturnsTasks()
    {
        // ФИКС: Используем It.IsAny<bool>(), так как сервис вызывает метод без 3-го параметра (default false)
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, It.IsAny<bool>()))
            .ReturnsAsync(new ProjectItem { Id = 1, UserId = UserId });
        
        _taskRepoMock.Setup(r => r.GetByProjectIdAsync(1, UserId))
            .ReturnsAsync(new List<TaskItem> { new() { Id = 10, Title = "Task" } });

        var result = await _service.GetProjectTasksAsync(1, UserId);
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProjectTasksAsync_ProjectNotFound_ThrowsKeyNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, UserId, It.IsAny<bool>())).ReturnsAsync((ProjectItem?)null);
        await _service.Invoking(s => s.GetProjectTasksAsync(99, UserId)).Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task GetProjectTasksAsync_EmptyProject_ReturnsEmptyList()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, It.IsAny<bool>())).ReturnsAsync(new ProjectItem { Id = 1, UserId = UserId });
        _taskRepoMock.Setup(r => r.GetByProjectIdAsync(1, UserId)).ReturnsAsync(new List<TaskItem>());

        var result = await _service.GetProjectTasksAsync(1, UserId);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjectByIdAsync_ExistingProject_ReturnsDto()
    {
        _repoMock.Setup(r => r.GetByIdAsync(1, UserId, true)).ReturnsAsync(new ProjectItem { Id = 1, UserId = UserId, Name = "P" });
        var result = await _service.GetProjectByIdAsync(1, UserId);
        result!.Name.Should().Be("P");
    }

    [Fact]
    public async Task UpdateProject_NonExisting_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, UserId, It.IsAny<bool>())).ReturnsAsync((ProjectItem?)null);
        var result = await _service.UpdateProjectAsync(99, new UpdateProjectDto { Name = "X" }, UserId);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProject_NonExisting_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetByIdAsync(99, UserId, It.IsAny<bool>())).ReturnsAsync((ProjectItem?)null);
        var result = await _service.DeleteProjectAsync(99, UserId);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateProject_SetsUserIdCorrectly()
    {
        ProjectItem? captured = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<ProjectItem>())).Callback<ProjectItem>(p => captured = p).ReturnsAsync((ProjectItem p) => p);
        await _service.CreateProjectAsync(new CreateProjectDto { Name = "P" }, UserId);
        captured!.UserId.Should().Be(UserId);
    }
}