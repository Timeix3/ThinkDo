// AppApi.Tests/Repositories/InboxRepositoryTests.cs
using AppApi.Repositories;
using AppApi.Tests.Helpers;
using Common.Models;
using FluentAssertions;

namespace AppApi.Tests.Repositories;

public class InboxRepositoryTests : IDisposable
{
    private readonly Common.Data.AppDbContext _context;
    private readonly InboxRepository _repository;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";
    private const int DefaultLimit = 20;

    public InboxRepositoryTests()
    {
        _context = DbContextHelper.CreateInMemoryContext();
        _repository = new InboxRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_WithUserItems_ReturnsOnlyUserItemsAndNotDeleted()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.InboxItems.AddRange(
            new InboxItem { Title = "Item 1", UserId = TestUserId, CreatedAt = now.AddHours(-2) },
            new InboxItem { Title = "Item 2", UserId = TestUserId, CreatedAt = now.AddHours(-1) },
            new InboxItem { Title = "Item 3", UserId = OtherUserId, CreatedAt = now },
            new InboxItem { Title = "Deleted Item", UserId = TestUserId, DeletedAt = now, CreatedAt = now.AddHours(-3) }
        );
        await _context.SaveChangesAsync();

        // Act
        var (result, hasOverflow) = await _repository.GetAllAsync(TestUserId, DefaultLimit);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(i => i.UserId == TestUserId && i.DeletedAt == null);
        hasOverflow.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_NoItemsForUser_ReturnsEmptyList()
    {
        // Arrange
        _context.InboxItems.Add(new InboxItem
        {
            Title = "Other User Item",
            UserId = OtherUserId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Act
        var (result, hasOverflow) = await _repository.GetAllAsync(TestUserId, DefaultLimit);

        // Assert
        result.Should().BeEmpty();
        hasOverflow.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_WithMoreThanLimit_ReturnsLimitedItemsAndOverflowTrue()
    {
        // Arrange
        var items = Enumerable.Range(1, 25)
            .Select(i => new InboxItem
            {
                Title = $"Item {i}",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            });
        _context.InboxItems.AddRange(items);
        await _context.SaveChangesAsync();

        // Act
        var (result, hasOverflow) = await _repository.GetAllAsync(TestUserId, 20);

        // Assert
        result.Should().HaveCount(20);
        hasOverflow.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllAsync_OrdersByCreatedAtDescending()
    {
        // Arrange
        var now = DateTime.UtcNow;
        _context.InboxItems.AddRange(
            new InboxItem { Title = "First", UserId = TestUserId, CreatedAt = now.AddHours(-2) },
            new InboxItem { Title = "Second", UserId = TestUserId, CreatedAt = now.AddHours(-1) },
            new InboxItem { Title = "Third", UserId = TestUserId, CreatedAt = now }
        );
        await _context.SaveChangesAsync();

        // Act
        var (result, _) = await _repository.GetAllAsync(TestUserId, DefaultLimit);
        var items = result.ToList();

        // Assert
        items.Should().HaveCount(3);
        items[0].Title.Should().Be("Third");
        items[1].Title.Should().Be("Second");
        items[2].Title.Should().Be("First");
    }

    [Fact]
    public async Task GetByIdAsync_ExistingItemAndCorrectUser_ReturnsItem()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Test Item",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(item.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Item");
        result.UserId.Should().Be(TestUserId);
        result.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingItemButWrongUser_ReturnsNull()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Test Item",
            UserId = OtherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(item.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedItem_ReturnsNull()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Deleted Item",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(item.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidItem_AddsToDatabase()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "New Item",
            UserId = TestUserId
        };

        // Act
        var result = await _repository.AddAsync(item);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Item");
        result.UserId.Should().Be(TestUserId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var inDb = await _context.InboxItems.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingItem_SetsDeletedAtAndReturnsTrue()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Item to Delete",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(item.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var inDb = await _context.InboxItems.FindAsync(item.Id);
        inDb.Should().NotBeNull();
        inDb!.DeletedAt.Should().NotBeNull();
        inDb.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingItemButWrongUser_ReturnsFalse()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Item to Delete",
            UserId = OtherUserId,
            CreatedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(item.Id, TestUserId);

        // Assert
        result.Should().BeFalse();

        var inDb = await _context.InboxItems.FindAsync(item.Id);
        inDb.Should().NotBeNull();
        inDb!.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_AlreadyDeletedItem_ReturnsFalse()
    {
        // Arrange
        var item = new InboxItem
        {
            Title = "Already Deleted",
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = DateTime.UtcNow
        };
        _context.InboxItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(item.Id, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_NonExistingItem_ReturnsFalse()
    {
        // Act
        var result = await _repository.SoftDeleteAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}