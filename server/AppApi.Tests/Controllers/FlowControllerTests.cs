using AppApi.Controllers;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class FlowControllerTests
{
    private readonly Mock<IFlowPhaseService> _serviceMock;
    private readonly Mock<ILogger<FlowController>> _loggerMock;
    private readonly FlowController _controller;
    private const string TestUserId = "test-user-123";

    public FlowControllerTests()
    {
        _serviceMock = new Mock<IFlowPhaseService>();
        _loggerMock = new Mock<ILogger<FlowController>>();
        _controller = new FlowController(_serviceMock.Object, _loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) }, "Test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetPhase_ExistingUser_ReturnsCurrentPhase()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetPhaseAsync(TestUserId))
            .ReturnsAsync("sprint");

        // Act
        var result = await _controller.GetPhase();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlowController.FlowPhaseResponse>().Subject;
        response.Phase.Should().Be("sprint");

        _serviceMock.Verify(s => s.GetPhaseAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetPhase_NewUser_ReturnsDefaultPlanning()
    {
        // Arrange
        _serviceMock
            .Setup(s => s.GetPhaseAsync(TestUserId))
            .ReturnsAsync("planning");

        // Act
        var result = await _controller.GetPhase();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlowController.FlowPhaseResponse>().Subject;
        response.Phase.Should().Be("planning");
    }

    [Fact]
    public async Task UpdatePhase_ValidPhase_ReturnsSuccess()
    {
        // Arrange
        var request = new FlowController.FlowPhaseUpdateRequest("sprint");
        _serviceMock
            .Setup(s => s.SetPhaseAsync(TestUserId, "sprint"))
            .ReturnsAsync("sprint");

        // Act
        var result = await _controller.UpdatePhase(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<FlowController.FlowPhaseUpdateResponse>().Subject;
        response.Success.Should().BeTrue();
        response.Phase.Should().Be("sprint");
    }

    [Fact]
    public async Task UpdatePhase_InvalidPhase_ReturnsBadRequest()
    {
        // Arrange
        var request = new FlowController.FlowPhaseUpdateRequest("invalid_phase");
        _serviceMock
            .Setup(s => s.SetPhaseAsync(TestUserId, "invalid_phase"))
            .ThrowsAsync(new ArgumentException("Invalid phase"));

        // Act
        var result = await _controller.UpdatePhase(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdatePhase_AllValidPhases_Accepted()
    {
        // Arrange
        var validPhases = new[] { "sprint", "review", "planning", "SPRINT", "REVIEW", "PLANNING" };

        foreach (var phase in validPhases)
        {
            var request = new FlowController.FlowPhaseUpdateRequest(phase);
            _serviceMock
                .Setup(s => s.SetPhaseAsync(TestUserId, phase))
                .ReturnsAsync(phase.ToLowerInvariant());

            // Act
            var result = await _controller.UpdatePhase(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }
    }

    [Fact]
    public void GetPhase_WithoutUser_ThrowsUnauthorized()
    {
        // Arrange
        var controllerWithoutUser = new FlowController(_serviceMock.Object, _loggerMock.Object);
        controllerWithoutUser.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act & Assert
        controllerWithoutUser
            .Invoking(c => c.GetPhase())
            .Should()
            .ThrowAsync<UnauthorizedAccessException>();
    }
}