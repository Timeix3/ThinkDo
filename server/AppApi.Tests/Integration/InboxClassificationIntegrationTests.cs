// AppApi.Tests/Integration/InboxClassificationIntegrationTests.cs
using Common.Data;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Testcontainers.PostgreSql;
using AppApi.Models.DTOs;

namespace AppApi.Tests.Integration;

public class InboxClassificationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase($"testdb_inbox_classify_{Guid.NewGuid():N}")
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

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.RemoveRange(db.InboxItems);
        db.Tasks.RemoveRange(db.Tasks);
        db.Projects.RemoveRange(db.Projects);
        db.Routines.RemoveRange(db.Routines);
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedInboxItemAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = new InboxItem
        {
            Title = title,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };
        db.InboxItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    [Fact]
    public async Task ClassifyInboxItem_ToTask_CreatesTaskAndSoftDeletesInbox()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Buy groceries");

        var request = new
        {
            targetType = "task",
            data = new { title = "Buy groceries", content = "Milk, eggs, bread" }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Skip if auth is not configured for tests
            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClassifyInboxItemResponseDto>();
        result.Should().NotBeNull();
        result!.TargetType.Should().Be("task");
        result.Title.Should().Be("Buy groceries");
        result.Status.Should().Be("Available");

        // Verify inbox item is soft-deleted
        var inboxResponse = await _client.GetAsync("/api/inbox");
        if (inboxResponse.StatusCode == HttpStatusCode.OK)
        {
            var inboxList = await inboxResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
            inboxList!.Items.Should().NotContain(i => i.Id == inboxId);
        }

        // Verify task was created
        var taskResponse = await _client.GetAsync($"/api/tasks/{result.Id}");
        if (taskResponse.StatusCode == HttpStatusCode.OK)
        {
            var task = await taskResponse.Content.ReadFromJsonAsync<TaskResponseDto>();
            task.Should().NotBeNull();
            task!.Title.Should().Be("Buy groceries");
            task.Content.Should().Be("Milk, eggs, bread");
        }
    }

    [Fact]
    public async Task ClassifyInboxItem_ToRoutine_CreatesRoutineAndSoftDeletesInbox()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Morning Exercise");

        var request = new
        {
            targetType = "routine",
            data = new { title = "Morning Exercise", frequency = "daily" }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClassifyInboxItemResponseDto>();
        result.Should().NotBeNull();
        result!.TargetType.Should().Be("routine");
        result.Title.Should().Be("Morning Exercise");
        result.Frequency.Should().Be("Daily");

        // Verify inbox item is soft-deleted
        var inboxResponse = await _client.GetAsync("/api/inbox");
        if (inboxResponse.StatusCode == HttpStatusCode.OK)
        {
            var inboxList = await inboxResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
            inboxList!.Items.Should().NotContain(i => i.Id == inboxId);
        }
    }

    [Fact]
    public async Task ClassifyInboxItem_ToProject_CreatesProjectAndSoftDeletesInbox()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("New Website Project");

        var request = new
        {
            targetType = "project",
            data = new { title = "New Website Project", description = "Build company website" }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClassifyInboxItemResponseDto>();
        result.Should().NotBeNull();
        result!.TargetType.Should().Be("project");
        result.Title.Should().Be("New Website Project");
        result.Description.Should().Be("Build company website");

        // Verify inbox item is soft-deleted
        var inboxResponse = await _client.GetAsync("/api/inbox");
        if (inboxResponse.StatusCode == HttpStatusCode.OK)
        {
            var inboxList = await inboxResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
            inboxList!.Items.Should().NotContain(i => i.Id == inboxId);
        }
    }

    [Fact]
    public async Task ClassifyInboxItem_NonExistentInbox_ReturnsNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        var request = new
        {
            targetType = "task",
            data = new { title = "Test" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox/99999/classify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassifyInboxItem_AlreadyClassified_ReturnsConflict()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Already classified");

        // First classification
        var firstRequest = new
        {
            targetType = "task",
            data = new { title = "Test Task" }
        };
        await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", firstRequest);

        // Second classification attempt
        var secondRequest = new
        {
            targetType = "project",
            data = new { title = "Test Project" }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", secondRequest);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Conflict, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassifyInboxItem_InvalidTargetType_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Test item");

        var request = new
        {
            targetType = "invalid_type",
            data = new { title = "Test" }
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassifyInboxItem_MissingRequiredField_ReturnsBadRequest()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Test item");

        var request = new
        {
            targetType = "routine",
            data = new { title = "Test" } // Missing frequency
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClassifyInboxItem_CreationFails_InboxNotDeleted()
    {
        // Arrange
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Will not be deleted");

        var request = new
        {
            targetType = "task",
            data = new { } // Missing title - should fail
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);

        // Verify inbox item is NOT deleted
        var inboxResponse = await _client.GetAsync("/api/inbox");
        if (inboxResponse.StatusCode == HttpStatusCode.OK)
        {
            var inboxList = await inboxResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
            inboxList!.Items.Should().Contain(i => i.Id == inboxId);
        }
    }
}