using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using AppApi.Repositories;
using FluentAssertions;

namespace AppApi.Tests.Repositories;

public class ProjectRepositoryTests
{
    private AppDbContext GetContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetAllAsync_ShouldOrderIsDefaultFirst()
    {
        // Проверяем сортировку: Текучка всегда сверху
        using var context = GetContext();
        var repo = new ProjectRepository(context);
        const string uid = "u1";

        context.Projects.AddRange(
            new ProjectItem { Name = "B", IsDefault = false, UserId = uid },
            new ProjectItem { Name = "Текучка", IsDefault = true, UserId = uid },
            new ProjectItem { Name = "A", IsDefault = false, UserId = uid }
        );
        await context.SaveChangesAsync();

        var result = (await repo.GetAllAsync(uid)).ToList();

        result[0].IsDefault.Should().BeTrue();
        result[0].Name.Should().Be("Текучка");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldNotReturnSoftDeleted()
    {
        // Краевой случай: Проект помечен как удаленный
        using var context = GetContext();
        var repo = new ProjectRepository(context);
        var project = new ProjectItem { Id = 1, UserId = "u1", DeletedAt = DateTime.UtcNow };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var result = await repo.GetByIdAsync(1, "u1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultProjectAsync_WithIncludeDeleted_ShouldFindSoftDeletedProject()
    {
        // Arrange
        using var context = GetContext();
        var repo = new ProjectRepository(context);
        var deletedProject = new ProjectItem
        {
            Id = 5,
            Name = "Текучка",
            UserId = "user-1",
            IsDefault = true,
            DeletedAt = DateTime.UtcNow
        };
        context.Projects.Add(deletedProject);
        await context.SaveChangesAsync();

        // Act
        // Обычный поиск не должен найти (т.к. есть глобальный фильтр на DeletedAt == null)
        var notFound = await repo.GetByIdAsync(5, "user-1");
        // Специальный поиск для восстановления должен найти
        var found = await repo.GetDefaultProjectAsync("user-1", includeDeleted: true);

        // Assert
        notFound.Should().BeNull();
        found.Should().NotBeNull();
        found!.Id.Should().Be(5);
    }
}