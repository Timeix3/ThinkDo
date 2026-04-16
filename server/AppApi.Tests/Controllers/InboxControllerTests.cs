// AppApi.Tests/Controllers/InboxControllerTests.cs
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
using System.Reflection;

namespace AppApi.Tests.Controllers;

public class InboxControllerTests
{
    private readonly Mock<IInboxService> _serviceMock;
    private readonly Mock<ILogger<InboxController>> _loggerMock;
    private readonly InboxController _controller;
    private const string TestUserId = "test-user-123";

    public InboxControllerTests()
    {
        _serviceMock = new Mock<IInboxService>();
        _loggerMock = new Mock<ILogger<InboxController>>();
        _controller = new InboxController(_serviceMock.Object, _loggerMock.Object);

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
    public async Task GetAll_ReturnsOkWithItems()
    {
        // Arrange
        var response = new InboxListResponseDto
        {
            Items = new List<InboxItemResponseDto>
            {
                new() { Id = 1, Title = "Item 1", CreatedAt = DateTime.UtcNow },
                new() { Id = 2, Title = "Item 2", CreatedAt = DateTime.UtcNow }
            },
            InboxOverflow = false
        };
        _serviceMock.Setup(s => s.GetAllItemsAsync(TestUserId)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InboxListResponseDto>().Subject;
        returnedResponse.Items.Should().HaveCount(2);
        returnedResponse.InboxOverflow.Should().BeFalse();
        _controller.Response.Headers["X-Inbox-Overflow"].ToString().Should().Be("false");
        _serviceMock.Verify(s => s.GetAllItemsAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithOverflow_SetsOverflowHeader()
    {
        // Arrange
        var response = new InboxListResponseDto
        {
            Items = new List<InboxItemResponseDto>
            {
                new() { Id = 1, Title = "Item 1", CreatedAt = DateTime.UtcNow }
            },
            InboxOverflow = true
        };
        _serviceMock.Setup(s => s.GetAllItemsAsync(TestUserId)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InboxListResponseDto>().Subject;
        returnedResponse.InboxOverflow.Should().BeTrue();
        _controller.Response.Headers["X-Inbox-Overflow"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        var response = new InboxListResponseDto
        {
            Items = new List<InboxItemResponseDto>(),
            InboxOverflow = false
        };
        _serviceMock.Setup(s => s.GetAllItemsAsync(TestUserId)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<InboxListResponseDto>().Subject;
        returnedResponse.Items.Should().BeEmpty();
        returnedResponse.InboxOverflow.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = "New Item" };
        var createdItem = new InboxItemResponseDto
        {
            Id = 1,
            Title = "New Item",
            CreatedAt = DateTime.UtcNow
        };
        _serviceMock.Setup(s => s.CreateItemAsync(dto, TestUserId)).ReturnsAsync(createdItem);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
        createdResult.Location.Should().Be("/api/inbox/1");
        var returnedItem = createdResult.Value.Should().BeOfType<InboxItemResponseDto>().Subject;
        returnedItem.Title.Should().Be("New Item");
        returnedItem.Id.Should().Be(1);
        _serviceMock.Verify(s => s.CreateItemAsync(dto, TestUserId), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task Create_EmptyOrWhitespaceTitle_ReturnsBadRequest(string invalidTitle)
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = invalidTitle };

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value;
        response.Should().NotBeNull();

        // Проверяем через рефлексию или сериализацию
        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("message");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("attempted to create inbox item with empty title")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        _serviceMock.Verify(s => s.CreateItemAsync(It.IsAny<CreateInboxItemDto>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Create_ServiceThrowsArgumentException_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = "Test" };
        _serviceMock.Setup(s => s.CreateItemAsync(dto, TestUserId))
            .ThrowsAsync(new ArgumentException("Title cannot be empty or whitespace"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var response = badRequestResult.Value;
        response.Should().NotBeNull();

        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("message");
    }

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteItemAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _serviceMock.Verify(s => s.SoftDeleteItemAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task Delete_NonExistingItem_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteItemAsync(999, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value;
        response.Should().NotBeNull();

        var json = JsonSerializer.Serialize(response);
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
    public async Task Delete_AlreadyDeletedItem_ReturnsNotFound()
    {
        // Arrange
        _serviceMock.Setup(s => s.SoftDeleteItemAsync(1, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAll_UserIdExtractedFromClaims()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetAllItemsAsync(TestUserId))
            .ReturnsAsync(new InboxListResponseDto { Items = new List<InboxItemResponseDto>() });

        // Act
        await _controller.GetAll();

        // Assert
        _serviceMock.Verify(s => s.GetAllItemsAsync(TestUserId), Times.Once);
    }

    [Fact]
    public void GetCurrentUserId_NoUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var controller = new InboxController(_serviceMock.Object, _loggerMock.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
        };

        // Act & Assert
        var method = typeof(InboxController).GetMethod("GetCurrentUserId",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var exception = Assert.Throws<TargetInvocationException>(() => method!.Invoke(controller, null));

        // Проверяем внутреннее исключение
        exception.InnerException.Should().BeOfType<UnauthorizedAccessException>();
        exception.InnerException!.Message.Should().Be("User ID not found in token");

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User ID not found in token claims")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}