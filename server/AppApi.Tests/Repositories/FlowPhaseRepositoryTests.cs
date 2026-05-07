using AppApi.Repositories;
using AppApi.Tests.Helpers;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Tests.Repositories;

public class FlowPhaseRepositoryTests : IDisposable
{
    private readonly Common.Data.AppDbContext _context;
    private readonly FlowPhaseRepository _repository;
    private const string TestUserId = "test-user-123";

    public FlowPhaseRepositoryTests()
    {
        _context = DbContextHelper.CreateInMemoryContext();
        _repository = new FlowPhaseRepository(_context);
    }

    [Fact]
    public async Task GetByUserIdAsync_ExistingRecord_ReturnsRecord()
    {
        // Arrange
        var phase = new UserFlowPhase
        {
            UserId = TestUserId,
            Phase = "review",
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserFlowPhases.Add(phase);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Phase.Should().Be("review");
        result.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public async Task GetByUserIdAsync_NewUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(TestUserId);

        // Assert (краевой случай: новый пользователь)
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_NewUser_CreatesRecord()
    {
        // Act
        await _repository.UpsertAsync(TestUserId, "sprint");

        // Assert
        var saved = await _context.UserFlowPhases
            .FirstOrDefaultAsync(x => x.UserId == TestUserId);

        saved.Should().NotBeNull();
        saved!.Phase.Should().Be("sprint");
        saved.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpsertAsync_ExistingUser_UpdatesPhase()
    {
        // Arrange
        var existing = new UserFlowPhase
        {
            UserId = TestUserId,
            Phase = "planning",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _context.UserFlowPhases.Add(existing);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        await _repository.UpsertAsync(TestUserId, "review");

        // Assert
        var updated = await _context.UserFlowPhases
            .FirstOrDefaultAsync(x => x.UserId == TestUserId);

        updated.Should().NotBeNull();
        updated!.Phase.Should().Be("review");
        updated.UpdatedAt.Should().BeAfter(existing.UpdatedAt);
    }

    [Fact]
    public async Task UpsertAsync_SamePhase_UpdatesTimestamp()
    {
        // Arrange
        var existing = new UserFlowPhase
        {
            UserId = TestUserId,
            Phase = "sprint",
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _context.UserFlowPhases.Add(existing);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var oldTimestamp = existing.UpdatedAt;

        // Act
        await _repository.UpsertAsync(TestUserId, "sprint");

        // Assert
        var updated = await _context.UserFlowPhases
            .FirstOrDefaultAsync(x => x.UserId == TestUserId);

        updated.Should().NotBeNull();
        updated!.Phase.Should().Be("sprint");
        updated.UpdatedAt.Should().BeAfter(oldTimestamp);
    }

    [Fact]
    public async Task UpsertAsync_UserIsolation_Respected()
    {
        // Arrange
        const string otherUserId = "other-user-456";

        await _repository.UpsertAsync(TestUserId, "sprint");
        await _repository.UpsertAsync(otherUserId, "review");

        // Assert
        var user1Phase = await _repository.GetByUserIdAsync(TestUserId);
        var user2Phase = await _repository.GetByUserIdAsync(otherUserId);

        user1Phase!.Phase.Should().Be("sprint");
        user2Phase!.Phase.Should().Be("review");
    }

    [Fact]
    public async Task UpsertAsync_ConcurrentAccess_LastWriteWins()
    {
        // Arrange (симуляция конкурентного доступа)
        await _repository.UpsertAsync(TestUserId, "sprint");

        // Симуляция запроса с другого устройства
        await _repository.UpsertAsync(TestUserId, "review");

        // Act
        var result = await _repository.GetByUserIdAsync(TestUserId);

        // Assert (последняя запись побеждает)
        result!.Phase.Should().Be("review");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}