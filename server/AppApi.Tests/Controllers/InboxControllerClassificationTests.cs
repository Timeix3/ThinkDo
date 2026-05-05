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

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) }, "TestAuth")) }
        };
    }

    private ClassifyInboxItemDto CreateRequest(string type, object data, string mode = "convert")
    {
        // Обновлено под новый контракт: entityType, mode, entityData
        var json = JsonSerializer.Serialize(new { entityType = type, mode = mode, entityData = data });
        return JsonSerializer.Deserialize<ClassifyInboxItemDto>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    [Fact]
    public async Task Classify_ValidTask_ReturnsOk()
    {
        // Arrange
        var request = CreateRequest("task", new { title = "New Task" });
        var response = new ClassifyResponseDto { Success = true, CreatedEntityId = 1, InboxDeleted = true };

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeOfType<ClassifyResponseDto>().Subject;
        returned.InboxDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Classify_ValidRoutine_ReturnsOk()
    {
        // Arrange
        var request = CreateRequest("routine", new { title = "Gym", frequency = "daily" });
        var response = new ClassifyResponseDto { Success = true, CreatedEntityId = 1, InboxDeleted = true };

        _classificationServiceMock
            .Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Classify(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Classify_NonExistentInbox_ReturnsNotFound()
    {
        var request = CreateRequest("task", new { title = "Test" });
        _classificationServiceMock.Setup(s => s.ClassifyInboxItemAsync(99, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new KeyNotFoundException());

        var result = await _controller.Classify(99, request);
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Classify_AlreadyClassified_ReturnsConflict()
    {
        var request = CreateRequest("task", new { title = "Test" });
        _classificationServiceMock.Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ThrowsAsync(new InvalidOperationException("already classified"));

        var result = await _controller.Classify(1, request);
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task Classify_ModeCreate_ReturnsInboxDeletedFalse()
    {
        var request = CreateRequest("task", new { title = "Template" }, "create");
        var response = new ClassifyResponseDto { Success = true, CreatedEntityId = 10, InboxDeleted = false };

        _classificationServiceMock.Setup(s => s.ClassifyInboxItemAsync(1, It.IsAny<ClassifyInboxItemDto>(), TestUserId))
            .ReturnsAsync(response);

        var result = await _controller.Classify(1, request);
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        ((ClassifyResponseDto)okResult.Value!).InboxDeleted.Should().BeFalse();
    }
}