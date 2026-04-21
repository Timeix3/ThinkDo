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
    private readonly ProjectService _service;
    private const string UserId = "user-123";

    public ProjectServiceTests()
    {
        _repoMock = new Mock<IProjectRepository>();
        _service = new ProjectService(_repoMock.Object);
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
}