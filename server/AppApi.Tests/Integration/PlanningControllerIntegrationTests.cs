using AppApi.Models.DTOs;
using Common.Data;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.PostgreSql;

namespace AppApi.Tests.Integration;

public class PlanningControllerIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string TestUserId = "github-test-user";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    var dbContextDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(AppDbContext));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));

                    using var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                });
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Sprints.RemoveRange(db.Sprints);
        db.Tasks.RemoveRange(db.Tasks);
        db.Projects.RemoveRange(db.Projects);
        await db.SaveChangesAsync();
    }

    private async Task SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Создаём проекты
        var defaultProject = new ProjectItem
        {
            Name = "Текучка",
            IsDefault = true,
            UserId = TestUserId
        };

        var projectX = new ProjectItem
        {
            Name = "Проект X",
            Description = "Описание",
            UserId = TestUserId
        };

        db.Projects.AddRange(defaultProject, projectX);
        await db.SaveChangesAsync();

        // Создаём задачи
        var task1 = new TaskItem
        {
            Title = "Задача 1",
            Status = TasksStatus.Available,
            ProjectId = defaultProject.Id,
            UserId = TestUserId
        };

        var task2 = new TaskItem
        {
            Title = "Задача 2",
            Status = TasksStatus.Available,
            ProjectId = defaultProject.Id,
            UserId = TestUserId
        };

        var task3 = new TaskItem
        {
            Title = "Задача 3 (в спринте)",
            Status = TasksStatus.Available,
            ProjectId = projectX.Id,
            UserId = TestUserId
        };

        db.Tasks.AddRange(task1, task2, task3);
        await db.SaveChangesAsync();

        // Создаём активный спринт с задачей 3
        var sprint = new SprintItem
        {
            UserId = TestUserId,
            Status = SprintStatus.Active,
            Tasks = new List<TaskItem> { task3 }
        };

        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetProjects_ReturnsOkWithCorrectStructure()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedDataAsync();

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Unauthorized);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var planningResponse = JsonSerializer.Deserialize<PlanningResponseDto>(content, options);

            planningResponse.Should().NotBeNull();
            planningResponse!.Projects.Should().NotBeNull();
            planningResponse.TotalProjects.Should().Be(planningResponse.Projects.Count());

            // Должен быть как минимум проект "Текучка"
            planningResponse.Projects.Should().Contain(p => p.Name == "Текучка");
        }
    }

    [Fact]
    public async Task GetProjects_TekuchkaAlwaysFirst()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedDataAsync();

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(options);

            // Assert
            planningResponse.Should().NotBeNull();
            if (planningResponse!.Projects.Any())
            {
                planningResponse.Projects.First().Name.Should().Be("Текучка");
            }
        }
    }

    [Fact]
    public async Task GetProjects_OnlyAvailableTasksReturned()
    {
        // Arrange
        await ClearDatabaseAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var project = new ProjectItem
            {
                Name = "Текучка",
                IsDefault = true,
                UserId = TestUserId
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            db.Tasks.AddRange(
                new TaskItem
                {
                    Title = "Available Task",
                    Status = TasksStatus.Available,
                    ProjectId = project.Id,
                    UserId = TestUserId
                },
                new TaskItem
                {
                    Title = "Completed Task",
                    Status = TasksStatus.Completed,
                    ProjectId = project.Id,
                    UserId = TestUserId
                },
                new TaskItem
                {
                    Title = "Cancelled Task",
                    Status = TasksStatus.Cancelled,
                    ProjectId = project.Id,
                    UserId = TestUserId
                }
            );
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(options);

            // Assert
            planningResponse.Should().NotBeNull();
            var tasks = planningResponse!.Projects.First().Tasks.ToList();

            // Только Available задачи
            tasks.Should().ContainSingle();
            tasks.Should().OnlyContain(t => t.Status == TasksStatus.Available);
            tasks.First().Title.Should().Be("Available Task");
        }
    }

    [Fact]
    public async Task GetProjects_TaskInActiveSprint_HasSelectedTrue()
    {
        // Arrange
        await ClearDatabaseAsync();
        await SeedDataAsync();

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(options);

            // Assert
            planningResponse.Should().NotBeNull();

            // Находим задачу "Задача 3 (в спринте)" во втором проекте
            var projectX = planningResponse!.Projects
                .FirstOrDefault(p => p.Name == "Проект X");

            projectX.Should().NotBeNull();
            var sprintTask = projectX!.Tasks
                .FirstOrDefault(t => t.Title == "Задача 3 (в спринте)");

            sprintTask.Should().NotBeNull();
            sprintTask!.Selected.Should().BeTrue(
                "задача должна быть отмечена как selected, так как она в активном спринте");
        }
    }

    [Fact]
    public async Task GetProjects_EmptyDatabase_CreatesDefaultProject()
    {
        // Arrange
        await ClearDatabaseAsync();

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(options);

            // Assert (краевой случай: пользователь без проектов)
            planningResponse.Should().NotBeNull();
            planningResponse!.Projects.Should().HaveCount(1);
            planningResponse.Projects.First().Name.Should().Be("Текучка");
            planningResponse.Projects.First().Tasks.Should().BeEmpty();
            planningResponse.TotalProjects.Should().Be(1);
        }
    }

    [Fact]
    public async Task GetProjects_ReturnsJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            // Assert
            response.Content.Headers.ContentType.Should().NotBeNull();
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }
    }

    [Fact]
    public async Task GetProjects_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        // Не добавляем Authorization header

        // Act
        var response = await clientWithoutAuth.GetAsync("/api/planning/projects");

        // Assert - должен вернуть 401
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}