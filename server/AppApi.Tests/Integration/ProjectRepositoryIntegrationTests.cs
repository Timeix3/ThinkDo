using AppApi.Repositories;
using Common.Data;
using Common.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AppApi.Tests.Integration;

public class ProjectRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task AddAsync_ShouldGenerateIdAndTimestamps()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repo = new ProjectRepository(context);

        var project = new ProjectItem { Name = "Real DB Test", UserId = "u1" };
        var result = await repo.AddAsync(project);

        result.Id.Should().BeGreaterThan(0);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteProject_ShouldLeaveTasksIntact()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var project = new ProjectItem { Name = "Container", UserId = "u1" };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var task = new TaskItem { Title = "Child", ProjectId = project.Id, UserId = "u1" };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        // Act: Мягкое удаление проекта
        project.DeletedAt = DateTime.UtcNow;
        context.Projects.Update(project);
        await context.SaveChangesAsync();

        var taskFromDb = await context.Tasks.FirstAsync(t => t.Title == "Child");
        taskFromDb.ProjectId.Should().Be(project.Id);
    }
}