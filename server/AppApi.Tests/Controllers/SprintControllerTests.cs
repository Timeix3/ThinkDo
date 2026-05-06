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
    private readonly Mock<IFlowPhaseService> _flowPhaseServiceMock;
    private readonly Mock<ILogger<SprintController>> _loggerMock;
    private readonly SprintController _controller;
    private const string TestUserId = "test-user-123";

    public SprintControllerTests()
    {
        _sprintServiceMock = new Mock<ISprintService>();
        _taskServiceMock = new Mock<ITaskService>();
        _flowPhaseServiceMock = new Mock<IFlowPhaseService>();
        _loggerMock = new Mock<ILogger<SprintController>>();

        _controller = new SprintController(
            _sprintServiceMock.Object,
            _taskServiceMock.Object,
            _flowPhaseServiceMock.Object,
            _loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId)
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetSprintTasks_ReturnsOk()
    {
        _taskServiceMock.Setup(s => s.GetSprintTasksAsync(TestUserId))
            .ReturnsAsync(new[] { new TaskResponseDto { Id = 1, Title = "T" } });

        var result = await _controller.GetSprintTasks();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetStatus_ReturnsOkWithStatus()
    {
        var status = new SprintStatusDto { Phase = "sprint", HasActiveSprint = true };
        _sprintServiceMock.Setup(s => s.GetStatusAsync(TestUserId)).ReturnsAsync(status);

        var result = await _controller.GetStatus();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(status);
    }

    [Fact]
    public async Task StartSprint_ValidRequest_ReturnsOk_AndUpdatesFlowPhase()
    {
        var request = new StartSprintRequestDto { TaskIds = new List<int> { 1, 2 } };
        var response = new StartSprintResponseDto { Success = true, SprintId = 1 };

        _sprintServiceMock.Setup(s => s.StartSprintAsync(request.TaskIds, TestUserId))
            .ReturnsAsync(response);

        _flowPhaseServiceMock.Setup(s => s.SetPhaseAsync(TestUserId, "sprint"))
            .ReturnsAsync("sprint");

        var result = await _controller.StartSprint(request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(response);

        _flowPhaseServiceMock.Verify(s => s.SetPhaseAsync(TestUserId, "sprint"), Times.Once);
    }

    [Fact]
    public async Task StartSprint_AlreadyActive_ReturnsConflict_AndDoesNotUpdateFlowPhase()
    {
        var request = new StartSprintRequestDto { TaskIds = new List<int> { 1 } };
        _sprintServiceMock.Setup(s => s.StartSprintAsync(It.IsAny<List<int>>(), TestUserId))
            .ThrowsAsync(new InvalidOperationException("Спринт уже запущен"));

        var result = await _controller.StartSprint(request);

        result.Should().BeOfType<ConflictObjectResult>();
        _flowPhaseServiceMock.Verify(s => s.SetPhaseAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartSprint_EmptyTasks_ReturnsBadRequest_AndDoesNotUpdateFlowPhase()
    {
        var request = new StartSprintRequestDto { TaskIds = new List<int>() };
        _sprintServiceMock.Setup(s => s.StartSprintAsync(It.IsAny<List<int>>(), TestUserId))
            .ThrowsAsync(new ArgumentException("Список задач пуст"));

        var result = await _controller.StartSprint(request);

        result.Should().BeOfType<BadRequestObjectResult>();
        _flowPhaseServiceMock.Verify(s => s.SetPhaseAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CompleteSprint_Valid_ReturnsOk_AndUpdatesFlowPhase()
    {
        var response = new CompleteSprintResponseDto { Success = true };
        _sprintServiceMock.Setup(s => s.CompleteSprintAsync(TestUserId))
            .ReturnsAsync(response);

        _flowPhaseServiceMock.Setup(s => s.SetPhaseAsync(TestUserId, "review"))
            .ReturnsAsync("review");

        var result = await _controller.CompleteSprint();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(response);

        _flowPhaseServiceMock.Verify(s => s.SetPhaseAsync(TestUserId, "review"), Times.Once);
    }

    [Fact]
    public async Task CompleteSprint_NoActiveSprint_ReturnsNotFound_AndDoesNotUpdateFlowPhase()
    {
        _sprintServiceMock.Setup(s => s.CompleteSprintAsync(TestUserId))
            .ThrowsAsync(new KeyNotFoundException("Нет активного спринта"));

        var result = await _controller.CompleteSprint();

        result.Should().BeOfType<NotFoundObjectResult>();
        _flowPhaseServiceMock.Verify(s => s.SetPhaseAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}