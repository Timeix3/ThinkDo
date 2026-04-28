// AppApi.Tests/Controllers/InboxControllerClassificationTests.cs
using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text.Json;

namespace AppApi.Tests.Controllers;

public class InboxControllerClassificationTests
{
    private readonly Mock<IInboxService> _inboxServiceMock;
    private readonly Mock<IInboxClassificationService> _classificationServiceMock;
    private readonly Mock<ILogger<InboxController>> _loggerMock;
    private readonly InboxController _controller;
    private const string TestUserId = "test-user-123";

    public InboxControllerClassificationTests()
    {
        _inboxServiceMock = new Mock<IInboxService>();
        _classificationServiceMock = new Mock<IInboxClassificationService>();
        _loggerMock = new Mock<ILogger<InboxController>>();

        _controller = new InboxController(
            _inboxServiceMock.Object,
            _classificationServiceMock.Object,
            _loggerMock.Object);

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

    private ClassifyInboxItemDto CreateRequest(string targetType, object data)
    {
        var json = JsonSerializer.Serialize(new { targetType, data });
        return JsonSerializer.Deserialize<ClassifyInboxItemDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task Classify_ValidTask_ReturnsOkWithCreatedEntity()
    {
        // Arrange
        var request = CreateRequest("task", new { title = "New Task" });
        var response = new ClassifyInboxItemResponseDto
        {
            Id = 1,
            TargetType = "task",
            Title = "New Task",
            Status = "Available",
            CreatedAt = DateTime.UtcNow
        };

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<ClassifyInboxItemResponseDto>().Subject;
        returnedResponse.TargetType.Should().Be("task");
        returnedResponse.Title.Should().Be("New Task");
    }

    [Fact]
    public async Task Classify_ValidRoutine_ReturnsOk()
    {
        // Arrange
        var request = CreateRequest("routine", new { title = "Morning Exercise", frequency = "daily" });
        var response = new ClassifyInboxItemResponseDto
        {
            Id = 1,
            TargetType = "routine",
            Title = "Morning Exercise",
            Frequency = "Daily",
            CreatedAt = DateTime.UtcNow
        };

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<ClassifyInboxItemResponseDto>().Subject;
        returnedResponse.TargetType.Should().Be("routine");
        returnedResponse.Frequency.Should().Be("Daily");
    }

    [Fact]
    public async Task Classify_NonExistentInbox_ReturnsNotFound()
    {
        // Arrange
        var request = CreateRequest("task", new { title = "Test" });

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(999, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new KeyNotFoundException("Inbox item with id '999' not found"));

        // Act
        var result = await _controller.Classify(999, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Classify_AlreadyClassified_ReturnsConflict()
    {
        // Arrange
        var request = CreateRequest("task", new { title = "Test" });

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new InvalidOperationException(
                "Inbox item with id '1' has already been classified or deleted"));

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var json = JsonSerializer.Serialize(conflictResult.Value);
        json.Should().Contain("already been classified");
    }

    [Fact]
    public async Task Classify_InvalidTargetType_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateRequest("invalid_type", new { title = "Test" });

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new ArgumentException("Invalid targetType: 'invalid_type'"));

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Classify_CreationError_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateRequest("task", new { title = "Test" });

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new InvalidOperationException("Failed to classify inbox item: Database error"));

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}