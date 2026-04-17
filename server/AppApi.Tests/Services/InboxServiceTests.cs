// AppApi.Tests/Services/InboxServiceTests.cs
using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class InboxServiceTests
{
    private readonly Mock<IInboxRepository> _repositoryMock;
    private readonly InboxService _service;
    private const string TestUserId = "test-user-123";
    private const int DefaultLimit = 20;

    public InboxServiceTests()
    {
        _repositoryMock = new Mock<IInboxRepository>();
        _service = new InboxService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllItemsAsync_WithItems_ReturnsItemsAndOverflowFlag()
    {
        // Arrange
        var items = new List<InboxItem>
        {
            new() { Id = 1, Title = "Item 1", UserId = TestUserId, CreatedAt = DateTime.UtcNow.AddHours(-1) },
            new() { Id = 2, Title = "Item 2", UserId = TestUserId, CreatedAt = DateTime.UtcNow }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId, DefaultLimit))
            .ReturnsAsync((items, false));

        // Act
        var result = await _service.GetAllItemsAsync(TestUserId);

        // Assert
        result.Items.Should().HaveCount(2);
        result.InboxOverflow.Should().BeFalse();
        result.Items.Should().AllBeOfType<InboxItemResponseDto>();
        result.Items.First().Title.Should().Be("Item 1");
        _repositoryMock.Verify(r => r.GetAllAsync(TestUserId, DefaultLimit), Times.Once);
    }

    [Fact]
    public async Task GetAllItemsAsync_WithOverflow_ReturnsOverflowTrue()
    {
        // Arrange
        var items = Enumerable.Range(1, 20)
            .Select(i => new InboxItem
            {
                Id = i,
                Title = $"Item {i}",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId, DefaultLimit))
            .ReturnsAsync((items, true));

        // Act
        var result = await _service.GetAllItemsAsync(TestUserId);

        // Assert
        result.Items.Should().HaveCount(20);
        result.InboxOverflow.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllItemsAsync_EmptyList_ReturnsEmptyListAndNoOverflow()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId, DefaultLimit))
            .ReturnsAsync((new List<InboxItem>(), false));

        // Act
        var result = await _service.GetAllItemsAsync(TestUserId);

        // Assert
        result.Items.Should().BeEmpty();
        result.InboxOverflow.Should().BeFalse();
    }

    [Fact]
    public async Task CreateItemAsync_ValidDto_ReturnsCreatedItem()
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = "  New Item  " };
        InboxItem? capturedItem = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<InboxItem>()))
            .Callback<InboxItem>(item => capturedItem = item)
            .ReturnsAsync((InboxItem item) =>
            {
                item.Id = 1;
                item.CreatedAt = DateTime.UtcNow;
                return item;
            });

        // Act
        var result = await _service.CreateItemAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Item"); // Trimmed
        result.Id.Should().Be(1);

        capturedItem.Should().NotBeNull();
        capturedItem!.Title.Should().Be("New Item");
        capturedItem.UserId.Should().Be(TestUserId);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<InboxItem>(
            i => i.Title == "New Item" && i.UserId == TestUserId)), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public async Task CreateItemAsync_EmptyOrWhitespaceTitle_ThrowsArgumentException(string invalidTitle)
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = invalidTitle };

        // Act
        Func<Task> act = () => _service.CreateItemAsync(dto, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Title cannot be empty or whitespace");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<InboxItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateItemAsync_TrimsTitle()
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = "  Test Title with Spaces  " };
        InboxItem? capturedItem = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<InboxItem>()))
            .Callback<InboxItem>(item => capturedItem = item)
            .ReturnsAsync((InboxItem item) => item);

        // Act
        await _service.CreateItemAsync(dto, TestUserId);

        // Assert
        capturedItem.Should().NotBeNull();
        capturedItem!.Title.Should().Be("Test Title with Spaces");
    }

    [Fact]
    public async Task SoftDeleteItemAsync_ExistingItem_ReturnsTrue()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.SoftDeleteItemAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteItemAsync_NonExistingItem_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(999, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _service.SoftDeleteItemAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(999, TestUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteItemAsync_AlreadyDeletedItem_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _service.SoftDeleteItemAsync(1, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllItemsAsync_RespectsDefaultLimit()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId, 20))
            .ReturnsAsync((new List<InboxItem>(), false));

        // Act
        await _service.GetAllItemsAsync(TestUserId);

        // Assert
        _repositoryMock.Verify(r => r.GetAllAsync(TestUserId, 20), Times.Once);
    }
}