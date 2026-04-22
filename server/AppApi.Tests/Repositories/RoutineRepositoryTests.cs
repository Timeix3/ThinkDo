using AppApi.Repositories;
using AppApi.Tests.Helpers;
using Common.Enums;
using Common.Models;
using FluentAssertions;

namespace AppApi.Tests.Repositories;

public class RoutineRepositoryTests : IDisposable
{
    private readonly Common.Data.AppDbContext _context;
    private readonly RoutineRepository _repository;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public RoutineRepositoryTests()
    {
        _context = DbContextHelper.CreateInMemoryContext();
        _repository = new RoutineRepository(_context);
    }

    [Fact]
    public async Task GetAllAsync_WithUserRoutines_ReturnsOnlyUserRoutinesAndNotDeleted()
    {
        // Arrange
        _context.Routines.AddRange(
            new Routine { Name = "Routine 1", Frequency = RoutineFrequency.Daily, UserId = TestUserId },
            new Routine { Name = "Routine 2", Frequency = RoutineFrequency.Weekly, UserId = TestUserId },
            new Routine { Name = "Routine 3", Frequency = RoutineFrequency.Monthly, UserId = OtherUserId },
            new Routine { Name = "Deleted Routine", Frequency = RoutineFrequency.Daily, UserId = TestUserId, DeletedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.UserId == TestUserId && r.DeletedAt == null);
    }

    [Fact]
    public async Task GetAllAsync_NoRoutinesForUser_ReturnsEmptyList()
    {
        // Arrange
        _context.Routines.Add(new Routine
        {
            Name = "Other User Routine",
            Frequency = RoutineFrequency.Daily,
            UserId = OtherUserId
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(TestUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRoutineAndCorrectUser_ReturnsRoutine()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Test Routine",
            Frequency = RoutineFrequency.Weekly,
            UserId = TestUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(routine.Id, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Routine");
        result.Frequency.Should().Be(RoutineFrequency.Weekly);
        result.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingRoutineButWrongUser_ReturnsNull()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Test Routine",
            Frequency = RoutineFrequency.Daily,
            UserId = OtherUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_DeletedRoutine_ReturnsNull()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Deleted Routine",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId,
            DeletedAt = DateTime.UtcNow
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_ValidRoutine_AddsToDatabase()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "New Routine",
            Frequency = RoutineFrequency.Monthly,
            UserId = TestUserId
        };

        // Act
        var result = await _repository.AddAsync(routine);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Routine");
        result.Frequency.Should().Be(RoutineFrequency.Monthly);
        result.UserId.Should().Be(TestUserId);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        var inDb = await _context.Routines.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingRoutineAndCorrectUser_UpdatesRoutine()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Original",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        var updatedRoutine = new Routine
        {
            Id = routine.Id,
            Name = "Updated",
            Frequency = RoutineFrequency.Weekly,
            UserId = TestUserId
        };

        // Act
        var result = await _repository.UpdateAsync(updatedRoutine, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated");
        result.Frequency.Should().Be(RoutineFrequency.Weekly);
        result.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateAsync_ExistingRoutineButWrongUser_ReturnsNull()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Original",
            Frequency = RoutineFrequency.Daily,
            UserId = OtherUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        var updatedRoutine = new Routine
        {
            Id = routine.Id,
            Name = "Updated",
            Frequency = RoutineFrequency.Weekly,
            UserId = TestUserId
        };

        // Act
        var result = await _repository.UpdateAsync(updatedRoutine, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingRoutine_SetsDeletedAtAndReturnsTrue()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Routine to Delete",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var inDb = await _context.Routines.FindAsync(routine.Id);
        inDb.Should().NotBeNull();
        inDb!.DeletedAt.Should().NotBeNull();
        inDb.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingRoutineButWrongUser_ReturnsFalse()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Routine to Delete",
            Frequency = RoutineFrequency.Daily,
            UserId = OtherUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeFalse();

        var inDb = await _context.Routines.FindAsync(routine.Id);
        inDb.Should().NotBeNull();
        inDb!.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_AlreadyDeletedRoutine_ReturnsFalse()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Already Deleted",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId,
            DeletedAt = DateTime.UtcNow
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.SoftDeleteAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_NonExistingRoutine_ReturnsFalse()
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