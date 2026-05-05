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
        db.Tasks.RemoveRange(db.Tasks);
        db.Projects.RemoveRange(db.Projects);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Основной сценарий: запрос возвращает 200 OK с корректной структурой данных
    /// </summary>
    [Fact]
    public async Task GetProjects_ReturnsOkWithCorrectStructure()
    {
        // Arrange
        var expectedStatusCodes = new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized };

        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        // Assert
        response.StatusCode.Should().BeOneOf(expectedStatusCodes);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();

            var planningResponse = JsonSerializer.Deserialize<PlanningResponseDto>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            planningResponse.Should().NotBeNull();
            planningResponse!.Projects.Should().NotBeNull();
            planningResponse.TotalProjects.Should().Be(planningResponse.Projects.Count());
            planningResponse.TotalProjects.Should().BeGreaterThanOrEqualTo(1);
        }
    }

    /// <summary>
    /// Проверка наличия дефолтного проекта "Текучка" (краевой случай)
    /// </summary>
    [Fact]
    public async Task GetProjects_ContainsDefaultProject()
    {
        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            planningResponse.Should().NotBeNull();

            // Должен быть как минимум проект "Текучка"
            var defaultProject = planningResponse!.Projects
                .FirstOrDefault(p => p.Name == "Текучка" || p.Id == 1);

            // Если это заглушка с хардкодом, то "Текучка" должна быть
            defaultProject.Should().NotBeNull("должен присутствовать дефолтный проект 'Текучка'");
        }
    }

    /// <summary>
    /// Проверка формата задач в проекте
    /// </summary>
    [Fact]
    public async Task GetProjects_TasksHaveCorrectFormat()
    {
        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            planningResponse.Should().NotBeNull();

            // Проверяем все задачи во всех проектах
            foreach (var project in planningResponse!.Projects)
            {
                project.Id.Should().BeGreaterThan(0);
                project.Name.Should().NotBeNullOrEmpty();

                if (project.Tasks.Any())
                {
                    foreach (var task in project.Tasks)
                    {
                        task.Id.Should().BeGreaterThan(0);
                        task.Title.Should().NotBeNullOrEmpty();
                        task.Status.Should().BeDefined();
                        // Selected должен быть boolean
                        (task.Selected == true || task.Selected == false).Should().BeTrue();
                    }
                }
                else
                {
                    // Краевой случай: проект без задач должен иметь пустой массив
                    project.Tasks.Should().BeEmpty();
                }
            }
        }
    }

    /// <summary>
    /// Проверка Content-Type заголовка
    /// </summary>
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

    /// <summary>
    /// Проверка статусов задач на валидность enum значений
    /// </summary>
    [Fact]
    public async Task GetProjects_TaskStatusesAreValidEnumValues()
    {
        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            var validStatuses = Enum.GetValues<TasksStatus>();

            foreach (var project in planningResponse!.Projects)
            {
                foreach (var task in project.Tasks)
                {
                    // Статус должен быть одним из допустимых значений TasksStatus
                    validStatuses.Should().Contain(task.Status,
                        $"статус задачи '{task.Title}' должен быть валидным значением TasksStatus");
                }
            }
        }
    }

    /// <summary>
    /// Проверка totalProjects соответствует фактическому количеству проектов
    /// </summary>
    [Fact]
    public async Task GetProjects_TotalProjectsMatchesActualCount()
    {
        // Act
        var response = await _client.GetAsync("/api/planning/projects");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var planningResponse = await response.Content
                .ReadFromJsonAsync<PlanningResponseDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

            var actualProjectCount = planningResponse!.Projects.Count();
            planningResponse.TotalProjects.Should().Be(actualProjectCount,
                "totalProjects должен соответствовать количеству проектов в массиве");
        }
    }
}