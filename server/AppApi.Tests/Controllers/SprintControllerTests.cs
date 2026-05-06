using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class SprintControllerTests
{
    private readonly Mock<ISprintService> _sprintServiceMock;
    private readonly Mock<ITaskService> _taskServiceMock;
    private readonly Mock<ILogger<SprintController>> _loggerMock;
    private readonly SprintController _controller;
    private const string TestUserId = "test-user-123";

    public SprintControllerTests()
    {
        _sprintServiceMock = new Mock<ISprintService>();
        _taskServiceMock = new Mock<ITaskService>();
        _loggerMock = new Mock<ILogger<SprintController>>();

        _controller = new SprintController(
            _sprintServiceMock.Object,
            _taskServiceMock.Object,
            _loggerMock.Object);

        // Настройка мока пользователя (User)
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    // --- ТЕСТЫ GET /tasks ---

    [Fact]
    public async Task GetSprintTasks_ReturnsOk()
    {
        // Arrange
        _taskServiceMock.Setup(s => s.GetSprintTasksAsync(TestUserId))
            .ReturnsAsync(new[] { new TaskResponseDto { Id = 1, Title = "T" } });

        // Act
        var result = await _controller.GetSprintTasks();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    // --- ТЕСТЫ GET /status  ---

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatus()
    {
        // Arrange
        var status = new SprintStatusDto { Phase = "sprint", HasActiveSprint = true };
        _sprintServiceMock.Setup(s => s.GetStatusAsync(TestUserId)).ReturnsAsync(status);

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(status);
    }

    // --- ТЕСТЫ POST /start  ---

    [Fact]
    public async Task StartSprint_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new StartSprintRequestDto { TaskIds = new List<int> { 1, 2 } };
        var response = new StartSprintResponseDto { Success = true, SprintId = 1 };

        _sprintServiceMock.Setup(s => s.StartSprintAsync(request.TaskIds, TestUserId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.StartSprint(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartSprint_AlreadyActive_ReturnsConflict()
    {
        // Arrange
        var request = new StartSprintRequestDto { TaskIds = new List<int> { 1 } };
        _sprintServiceMock.Setup(s => s.StartSprintAsync(It.IsAny<List<int>>(), TestUserId))
            .ThrowsAsync(new InvalidOperationException("Спринт уже запущен"));

        // Act
        var result = await _controller.StartSprint(request);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task StartSprint_EmptyTasks_ReturnsBadRequest()
    {
        // Arrange
        var request = new StartSprintRequestDto { TaskIds = new List<int>() };
        _sprintServiceMock.Setup(s => s.StartSprintAsync(It.IsAny<List<int>>(), TestUserId))
            .ThrowsAsync(new ArgumentException("Список задач пуст"));

        // Act
        var result = await _controller.StartSprint(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --- ТЕСТЫ POST /complete  ---

    [Fact]
    public async Task CompleteSprint_Valid_ReturnsOk()
    {
        // Arrange
        _sprintServiceMock.Setup(s => s.CompleteSprintAsync(TestUserId))
            .ReturnsAsync(new CompleteSprintResponseDto { Success = true });

        // Act
        var result = await _controller.CompleteSprint();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CompleteSprint_NoActiveSprint_ReturnsNotFound()
    {
        // Arrange
        _sprintServiceMock.Setup(s => s.CompleteSprintAsync(TestUserId))
            .ThrowsAsync(new KeyNotFoundException("Нет активного спринта"));

        // Act
        var result = await _controller.CompleteSprint();

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}