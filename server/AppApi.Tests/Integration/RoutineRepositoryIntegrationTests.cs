using AppApi.Repositories;
using Common.Data;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppApi.Tests.Integration;

public class RoutineRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    private AppDbContext _context = null!;
    private RoutineRepository _repository = null!;
    private const string TestUserId = "test-user-123";
    private const string OtherUserId = "other-user-456";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
        _repository = new RoutineRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
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
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.GetAllAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(r => r.UserId == TestUserId && r.DeletedAt == null);
    }

    [Fact]
    public async Task AddAsync_ValidRoutine_PersistsToDatabase()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Integration Test Routine",
            Frequency = RoutineFrequency.Weekly,
            UserId = TestUserId
        };

        // Act
        var result = await _repository.AddAsync(routine);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var inDb = await _context.Routines.FindAsync(result.Id);
        inDb.Should().NotBeNull();
        inDb!.Name.Should().Be("Integration Test Routine");
        inDb.Frequency.Should().Be(RoutineFrequency.Weekly);
        inDb.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task UpdateAsync_ExistingRoutine_UpdatesInDatabase()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Original Name",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updatedRoutine = new Routine
        {
            Id = routine.Id,
            Name = "Updated Name",
            Frequency = RoutineFrequency.Monthly,
            UserId = TestUserId
        };

        // Act
        var result = await _repository.UpdateAsync(updatedRoutine, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Frequency.Should().Be(RoutineFrequency.Monthly);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        var inDb = await _context.Routines.FindAsync(routine.Id);
        inDb!.Name.Should().Be("Updated Name");
        inDb.Frequency.Should().Be(RoutineFrequency.Monthly);
    }

    [Fact]
    public async Task SoftDeleteAsync_ExistingRoutine_SetsDeletedAt()
    {
        // Arrange
        var routine = new Routine
        {
            Name = "Routine to Soft Delete",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId
        };
        _context.Routines.Add(routine);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = await _repository.SoftDeleteAsync(routine.Id, TestUserId);

        // Assert
        result.Should().BeTrue();

        var inDb = await _context.Routines.FindAsync(routine.Id);
        inDb.Should().NotBeNull();
        inDb!.DeletedAt.Should().NotBeNull();
        inDb.DeletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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
}