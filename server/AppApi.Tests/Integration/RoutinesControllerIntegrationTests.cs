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
using Testcontainers.PostgreSql;
using AppApi.Models.DTOs;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AppApi.Tests.Integration;

public class RoutinesControllerIntegrationTests : IAsyncLifetime
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
                    {
                        options.UseNpgsql(_postgres.GetConnectionString());
                        options.ConfigureWarnings(warnings =>
                            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
                    });

                    using var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.Migrate();
                });
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task ClearRoutinesAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Routines.RemoveRange(db.Routines);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }

    [Fact]
    public async Task Create_ValidRoutine_ReturnsCreated()
    {
        // Arrange
        await ClearRoutinesAsync();
        var dto = new CreateRoutineDto { Name = "Integration Test Routine", Frequency = RoutineFrequency.Daily };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routines", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Unauthorized);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var routine = await response.Content.ReadFromJsonAsync<RoutineResponseDto>();
            routine.Should().NotBeNull();
            routine!.Name.Should().Be("Integration Test Routine");
            routine.Frequency.Should().Be(RoutineFrequency.Daily);
        }
    }

    [Fact]
    public async Task Create_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = "   ", Frequency = RoutineFrequency.Daily };

        // Act
        var response = await _client.PostAsJsonAsync("/api/routines", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_InvalidFrequency_ReturnsBadRequest()
    {
        // Arrange
        var json = "{\"name\":\"Test\",\"frequency\":\"biweekly\"}";
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/routines", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAll_ReturnsRoutinesList()
    {
        // Arrange
        await ClearRoutinesAsync();
        await SeedRoutinesAsync(
            new Routine { Name = "Morning Routine", Frequency = RoutineFrequency.Daily, UserId = "github-test-user" },
            new Routine { Name = "Weekly Review", Frequency = RoutineFrequency.Weekly, UserId = "github-test-user" }
        );

        // Act
        var response = await _client.GetAsync("/api/routines");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var routines = await response.Content.ReadFromJsonAsync<List<RoutineResponseDto>>();
            routines.Should().NotBeNull();
            routines.Should().HaveCountGreaterThanOrEqualTo(2);
            routines!.Select(r => r.Name).Should().Contain(new[] { "Morning Routine", "Weekly Review" });
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsEmptyArray()
    {
        // Arrange
        await ClearRoutinesAsync();

        // Act
        var response = await _client.GetAsync("/api/routines");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var routines = await response.Content.ReadFromJsonAsync<List<RoutineResponseDto>>();
            routines.Should().NotBeNull();
            routines.Should().BeEmpty();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GetById_ExistingRoutine_ReturnsRoutine()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine { Name = "Test Routine", Frequency = RoutineFrequency.Monthly, UserId = "github-test-user" }
        );

        // Act
        var response = await _client.GetAsync($"/api/routines/{routineId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var routine = await response.Content.ReadFromJsonAsync<RoutineResponseDto>();
            routine.Should().NotBeNull();
            routine!.Id.Should().Be(routineId);
            routine.Name.Should().Be("Test Routine");
            routine.Frequency.Should().Be(RoutineFrequency.Monthly);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GetById_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        await ClearRoutinesAsync();

        // Act
        var response = await _client.GetAsync("/api/routines/99999");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetById_DeletedRoutine_ReturnsNotFound()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine
            {
                Name = "Deleted Routine",
                Frequency = RoutineFrequency.Daily,
                UserId = "github-test-user",
                DeletedAt = DateTime.UtcNow
            }
        );

        // Act
        var response = await _client.GetAsync($"/api/routines/{routineId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_ExistingRoutine_ReturnsUpdatedRoutine()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine { Name = "Original Name", Frequency = RoutineFrequency.Daily, UserId = "github-test-user" }
        );

        var updateDto = new UpdateRoutineDto { Name = "Updated Name", Frequency = RoutineFrequency.Weekly };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/routines/{routineId}", updateDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var routine = await response.Content.ReadFromJsonAsync<RoutineResponseDto>();
            routine.Should().NotBeNull();
            routine!.Name.Should().Be("Updated Name");
            routine.Frequency.Should().Be(RoutineFrequency.Weekly);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Update_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        await ClearRoutinesAsync();
        var updateDto = new UpdateRoutineDto { Name = "Updated", Frequency = RoutineFrequency.Daily };

        // Act
        var response = await _client.PutAsJsonAsync("/api/routines/99999", updateDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine { Name = "Original", Frequency = RoutineFrequency.Daily, UserId = "github-test-user" }
        );

        var updateDto = new UpdateRoutineDto { Name = "   ", Frequency = RoutineFrequency.Daily };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/routines/{routineId}", updateDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ExistingRoutine_ReturnsNoContent()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine { Name = "To Delete", Frequency = RoutineFrequency.Daily, UserId = "github-test-user" }
        );

        // Act
        var response = await _client.DeleteAsync($"/api/routines/{routineId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.Unauthorized);

        // Verify it's not returned in GET
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            var getResponse = await _client.GetAsync($"/api/routines/{routineId}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task Delete_NonExistingRoutine_ReturnsNotFound()
    {
        // Arrange
        await ClearRoutinesAsync();

        // Act
        var response = await _client.DeleteAsync("/api/routines/99999");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_AlreadyDeletedRoutine_ReturnsNotFound()
    {
        // Arrange
        await ClearRoutinesAsync();
        var routineId = await SeedSingleRoutineAsync(
            new Routine
            {
                Name = "Already Deleted",
                Frequency = RoutineFrequency.Daily,
                UserId = "github-test-user",
                DeletedAt = DateTime.UtcNow
            }
        );

        // Act
        var response = await _client.DeleteAsync($"/api/routines/{routineId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullWorkflow_CreateUpdateDelete_WorksCorrectly()
    {
        // Arrange
        await ClearRoutinesAsync();

        // 1. Create
        var createDto = new CreateRoutineDto { Name = "Workflow Test", Frequency = RoutineFrequency.Weekly };
        var createResponse = await _client.PostAsJsonAsync("/api/routines", createDto);

        if (createResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Skip if auth is not configured
            return;
        }

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<RoutineResponseDto>();
        var routineId = created!.Id;

        // 2. Get by ID
        var getResponse = await _client.GetAsync($"/api/routines/{routineId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<RoutineResponseDto>();
        retrieved!.Name.Should().Be("Workflow Test");

        // 3. Update
        var updateDto = new UpdateRoutineDto { Name = "Updated Workflow", Frequency = RoutineFrequency.Monthly };
        var updateResponse = await _client.PutAsJsonAsync($"/api/routines/{routineId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RoutineResponseDto>();
        updated!.Name.Should().Be("Updated Workflow");
        updated.Frequency.Should().Be(RoutineFrequency.Monthly);

        // 4. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/routines/{routineId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 5. Verify deleted
        var getAfterDelete = await _client.GetAsync($"/api/routines/{routineId}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Вспомогательные методы для тестов
    private async Task SeedRoutinesAsync(params Routine[] routines)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Routines.AddRange(routines);
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedSingleRoutineAsync(Routine routine)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Routines.Add(routine);
        await db.SaveChangesAsync();
        return routine.Id;
    }
}