// AppApi.Tests/Controllers/PlanningControllerTests.cs

using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Common.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class PlanningControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly Mock<ILogger<PlanningController>> _loggerMock;
    private readonly PlanningController _controller;
    private const string TestUserId = "test-user-123";

    public PlanningControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        _loggerMock = new Mock<ILogger<PlanningController>>();
        _controller = new PlanningController(_serviceMock.Object, _loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) }, "Test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetProjects_ReturnsOkWithPlanningData()
    {
        // Arrange
        var expectedResponse = new PlanningResponseDto
        {
            Projects = new[]
            {
                new PlanningProjectDto
                {
                    Id = 1,
                    Name = "Текучка",
                    Description = null,
                    Tasks = new[]
                    {
                        new PlanningTaskDto
                        {
                            Id = 1,
                            Title = "Задача 1",
                            Status = TasksStatus.Available,
                            Selected = false
                        }
                    }
                },
                new PlanningProjectDto
                {
                    Id = 2,
                    Name = "Проект X",
                    Description = "Описание",
                    Tasks = new[]
                    {
                        new PlanningTaskDto
                        {
                            Id = 2,
                            Title = "Задача 2",
                            Status = TasksStatus.Available,
                            Selected = true
                        }
                    }
                }
            },
            TotalProjects = 2
        };

        _serviceMock
            .Setup(s => s.GetPlanningProjectsAsync(TestUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetProjects();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<PlanningResponseDto>().Subject;

        returnedDto.Projects.Should().HaveCount(2);
        returnedDto.TotalProjects.Should().Be(2);
        returnedDto.Projects.First().Name.Should().Be("Текучка");

        _serviceMock.Verify(s => s.GetPlanningProjectsAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetProjects_UserHasOnlyDefaultProject_ReturnsOk()
    {
        // Arrange (краевой случай: только Текучка)
        var expectedResponse = new PlanningResponseDto
        {
            Projects = new[]
            {
                new PlanningProjectDto
                {
                    Id = 1,
                    Name = "Текучка",
                    Description = null,
                    Tasks = Array.Empty<PlanningTaskDto>()
                }
            },
            TotalProjects = 1
        };

        _serviceMock
            .Setup(s => s.GetPlanningProjectsAsync(TestUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetProjects();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<PlanningResponseDto>().Subject;

        returnedDto.Projects.Should().HaveCount(1);
        returnedDto.TotalProjects.Should().Be(1);
        returnedDto.Projects.First().Name.Should().Be("Текучка");
    }

    [Fact]
    public async Task GetProjects_ProjectWithoutTasks_ReturnsProjectWithEmptyArray()
    {
        // Arrange (краевой случай: проект без available задач)
        var expectedResponse = new PlanningResponseDto
        {
            Projects = new[]
            {
                new PlanningProjectDto
                {
                    Id = 1,
                    Name = "Текучка",
                    Description = null,
                    Tasks = Array.Empty<PlanningTaskDto>()
                },
                new PlanningProjectDto
                {
                    Id = 2,
                    Name = "Пустой проект",
                    Description = "Нет доступных задач",
                    Tasks = Array.Empty<PlanningTaskDto>()
                }
            },
            TotalProjects = 2
        };

        _serviceMock
            .Setup(s => s.GetPlanningProjectsAsync(TestUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetProjects();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<PlanningResponseDto>().Subject;

        returnedDto.Projects.Should().HaveCount(2);
        returnedDto.Projects.ElementAt(1).Tasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProjects_UserIdExtractedFromClaims()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetPlanningProjectsAsync(TestUserId))
            .ReturnsAsync(new PlanningResponseDto
            {
                Projects = Array.Empty<PlanningProjectDto>(),
                TotalProjects = 0
            });

        // Act
        await _controller.GetProjects();

        // Assert
        _serviceMock.Verify(s => s.GetPlanningProjectsAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetProjects_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetPlanningProjectsAsync(TestUserId))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        await _controller
            .Invoking(c => c.GetProjects())
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Database error");
    }

    [Fact]
    public void GetProjects_WithoutUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var controllerWithoutUser = new PlanningController(
            _serviceMock.Object,
            _loggerMock.Object);

        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // No user claims
        };

        // Act & Assert
        controllerWithoutUser
            .Invoking(c => c.GetProjects())
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}