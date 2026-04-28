using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly ProjectsController _controller;
    private const string UserId = "github-1";

    public ProjectsControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        var loggerMock = new Mock<ILogger<ProjectsController>>();
        _controller = new ProjectsController(_serviceMock.Object, loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, UserId)
    }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost/api/projects/1");
        _controller.Url = urlHelperMock.Object;
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var dto = new CreateProjectDto { Name = "" };

        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.Create(dto);

        _serviceMock.Verify(s => s.CreateProjectAsync(dto, UserId), Times.Never);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.UpdateProjectAsync(99, It.IsAny<UpdateProjectDto>(), UserId))
            .ReturnsAsync((ProjectResponseDto?)null);

        var result = await _controller.Update(99, new UpdateProjectDto { Name = "X" });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_DifferentUser_ReturnsNotFound()
    {
        // Arrange: Юзер "github-1" просит проект, который принадлежит "github-2"
        _serviceMock.Setup(s => s.GetProjectByIdAsync(1, UserId)).ReturnsAsync((ProjectResponseDto?)null);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_WhenServiceThrowsException_ReturnsBadRequest()
    {
        // Arrange: Сервис выбрасывает ошибку (например, нарушение бизнес-правила)
        var dto = new UpdateProjectDto { Name = "Wrong" };
        _serviceMock.Setup(s => s.UpdateProjectAsync(1, dto, UserId))
                    .ThrowsAsync(new InvalidOperationException("Критическая ошибка бизнес-логики"));

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequest.Value as dynamic;
        string message = response.GetType().GetProperty("message").GetValue(response, null);
        message.Should().Be("Критическая ошибка бизнес-логики");
    }

    [Fact]
    public async Task GetProjectTasks_ValidProject_ReturnsOkWithTasks()
    {
        // Arrange
        var tasks = new List<TaskResponseDto> { new() { Id = 1, Title = "Project Task" } };
        _serviceMock.Setup(s => s.GetProjectTasksAsync(1, UserId)).ReturnsAsync(tasks);

        // Act
        var result = await _controller.GetProjectTasks(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = okResult.Value as IEnumerable<TaskResponseDto>;
        value.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetProjectTasks_StrangerProject_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetProjectTasksAsync(99, UserId))
            .ThrowsAsync(new KeyNotFoundException("Project not found"));

        // Act
        var result = await _controller.GetProjectTasks(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}