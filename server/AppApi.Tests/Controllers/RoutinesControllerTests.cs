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
using System.Text.Json;

namespace AppApi.Tests.Controllers;

public class RoutinesControllerTests
{
    private readonly Mock<IRoutineService> _serviceMock;
    private readonly Mock<ILogger<RoutinesController>> _loggerMock;
    private readonly RoutinesController _controller;
    private const string TestUserId = "test-user-123";

    public RoutinesControllerTests()
    {
        _serviceMock = new Mock<IRoutineService>();
        _loggerMock = new Mock<ILogger<RoutinesController>>();
        _controller = new RoutinesController(_serviceMock.Object, _loggerMock.Object);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithRoutines()
    {
        // Arrange
        var routines = new List<RoutineResponseDto>
        {
            new() { Id = 1, Name = "Morning Exercise", Frequency = RoutineFrequency.Daily, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Weekly Review", Frequency = RoutineFrequency.Weekly, CreatedAt = DateTime.UtcNow }
        };
        _serviceMock.Setup(s => s.GetAllRoutinesAsync(TestUserId)).ReturnsAsync(routines);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoutines = okResult.Value.Should().BeAssignableTo<IEnumerable<RoutineResponseDto>>().Subject;
        returnedRoutines.Should().HaveCount(2);
        _serviceMock.Verify(s => s.GetAllRoutinesAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllRoutinesAsync(TestUserId)).ReturnsAsync(new List<RoutineResponseDto>());

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoutines = okResult.Value.Should().BeAssignableTo<IEnumerable<RoutineResponseDto>>().Subject;
        returnedRoutines.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingRoutine_ReturnsOkWithRoutine()
    {
        // Arrange
        var routine = new RoutineResponseDto
        {
            Id = 1,
            Name = "Morning Exercise",
            Frequency = RoutineFrequency.Daily,
            CreatedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.GetRoutineByIdAsync(1, TestUserId)).ReturnsAsync(routine);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoutine = okResult.Value.Should().BeOfType<RoutineResponseDto>().Subject;
        returnedRoutine.Id.Should().Be(1);
        returnedRoutine.Name.Should().Be("Morning Exercise");
        returnedRoutine.Frequency.Should().Be(RoutineFrequency.Daily);
    }

    [Fact]
    public async Task GetById_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetRoutineByIdAsync(999, TestUserId)).ReturnsAsync((RoutineResponseDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = "Morning Exercise", Frequency = RoutineFrequency.Daily };
        var createdRoutine = new RoutineResponseDto
        {
            Id = 1,
            Name = "Morning Exercise",
            Frequency = RoutineFrequency.Daily,
            CreatedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.CreateRoutineAsync(dto, TestUserId)).ReturnsAsync(createdRoutine);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetById");
        createdResult.RouteValues!["id"].Should().Be(1);
        var returnedRoutine = createdResult.Value.Should().BeOfType<RoutineResponseDto>().Subject;
        returnedRoutine.Name.Should().Be("Morning Exercise");
        returnedRoutine.Frequency.Should().Be(RoutineFrequency.Daily);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Create_EmptyOrWhitespaceName_ReturnsBadRequest(string invalidName)
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = invalidName, Frequency = RoutineFrequency.Daily };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        jsonElement.TryGetProperty("message", out var messageProperty).Should().BeTrue();
        messageProperty.GetString().Should().Be("Name cannot be empty or contain only whitespace");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to create routine with empty name")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        _serviceMock.Verify(s => s.CreateRoutineAsync(It.IsAny<CreateRoutineDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = "Test", Frequency = RoutineFrequency.Daily };
        _serviceMock.Setup(s => s.CreateRoutineAsync(dto, TestUserId))
            .ThrowsAsync(new ArgumentException("Name cannot be empty or whitespace"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        json.Should().Contain("message");
    }

    [Fact]
    public async Task Update_ExistingRoutine_ReturnsOkWithUpdatedRoutine()
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = "Evening Exercise", Frequency = RoutineFrequency.Daily };
        var updatedRoutine = new RoutineResponseDto
        {
            Id = 1,
            Name = "Evening Exercise",
            Frequency = RoutineFrequency.Daily,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.UpdateRoutineAsync(1, dto, TestUserId)).ReturnsAsync(updatedRoutine);

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRoutine = okResult.Value.Should().BeOfType<RoutineResponseDto>().Subject;
        returnedRoutine.Name.Should().Be("Evening Exercise");
        returnedRoutine.Frequency.Should().Be(RoutineFrequency.Daily);
        returnedRoutine.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = "Updated", Frequency = RoutineFrequency.Weekly };
        _serviceMock.Setup(s => s.UpdateRoutineAsync(999, dto, TestUserId)).ReturnsAsync((RoutineResponseDto?)null);

        // Act
        var result = await _controller.Update(999, dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Update_EmptyOrWhitespaceName_ReturnsBadRequest(string invalidName)
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = invalidName, Frequency = RoutineFrequency.Daily };

        // Act
        var result = await _controller.Update(1, dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var json = JsonSerializer.Serialize(badRequestResult.Value);
        var jsonElement = JsonDocument.Parse(json).RootElement;

        jsonElement.TryGetProperty("message", out var messageProperty).Should().BeTrue();
        messageProperty.GetString().Should().Be("Name cannot be empty or contain only whitespace");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to update routine") && v.ToString()!.Contains("empty name")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        _serviceMock.Verify(s => s.UpdateRoutineAsync(It.IsAny<int>(), It.IsAny<UpdateRoutineDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ExistingRoutine_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteRoutineAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteRoutineAsync(999, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var json = JsonSerializer.Serialize(notFoundResult.Value);
        json.Should().Contain("message");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found or already deleted")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task Delete_AlreadyDeletedRoutine_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteRoutineAsync(1, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAll_UserIdExtractedFromClaims()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllRoutinesAsync(TestUserId)).ReturnsAsync(new List<RoutineResponseDto>());

        // Act
        await _controller.GetAll();

        // Assert
        _serviceMock.Verify(s => s.GetAllRoutinesAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetAll_NoUserIdInClaims_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var controller = new RoutinesController(_serviceMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act
        Func<Task> act = () => controller.GetAll();

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("User ID not found in token");
    }
}