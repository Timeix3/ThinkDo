using AppApi.Repositories;
using Common.Data;
using Common.Models;
using Common.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AppApi.Tests.Integration;

public class SprintRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    private async Task<AppDbContext> CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    [Fact]
    public async Task AddAsync_TwoActiveSprints_ShouldViolateUniqueIndex()
    {
        // Arrange
        using var context = await CreateContext();
        var repo = new SprintRepository(context);
        const string uid = "user-1";

        var sprint1 = new SprintItem { UserId = uid, Status = SprintStatus.Active };
        await repo.AddAsync(sprint1);

        // Act: Пытаемся создать второй активный спринт для того же юзера
        var sprint2 = new SprintItem { UserId = uid, Status = SprintStatus.Active };
        
        // Assert: База данных должна выкинуть ошибку (DbUpdateException)
        await repo.Invoking(r => r.AddAsync(sprint2))
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task GetActiveSprint_ShouldIncludeTasks_ManyToMany()
    {
        // Arrange
        using var context = await CreateContext();
        var repo = new SprintRepository(context);
        const string uid = "user-1";

        var task = new TaskItem { Title = "Sprint Task", UserId = uid };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var sprint = new SprintItem { UserId = uid, Status = SprintStatus.Active };
        sprint.Tasks.Add(task);
        await repo.AddAsync(sprint);
        context.ChangeTracker.Clear();

        // Act
        var result = await repo.GetActiveSprintAsync(uid);

        // Assert
        result.Should().NotBeNull();
        result!.Tasks.Should().HaveCount(1);
        result.Tasks.First().Title.Should().Be("Sprint Task");
    }
}