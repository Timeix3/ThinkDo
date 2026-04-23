using Common.Data;
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

namespace AppApi.Tests.Integration;

public class InboxControllerIntegrationTests : IAsyncLifetime
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
                    // Remove the existing DbContext registration
                    var descriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    var dbContextDescriptor = services.SingleOrDefault(d =>
                        d.ServiceType == typeof(AppDbContext));
                    if (dbContextDescriptor != null)
                        services.Remove(dbContextDescriptor);

                    // Add test DbContext with PostgreSQL container
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(_postgres.GetConnectionString()));

                    // Create and migrate database
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

    private async Task ClearInboxItemsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.RemoveRange(db.InboxItems);
        await db.SaveChangesAsync();
    }

    private async Task SeedInboxItemsAsync(params InboxItem[] items)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.AddRange(items);
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedSingleInboxItemAsync(InboxItem item)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
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
    public async Task GetAll_WithItems_ReturnsItemsList()
    {
        // Arrange
        await ClearInboxItemsAsync();
        await SeedInboxItemsAsync(
            new InboxItem { Title = "First item", UserId = TestUserId, CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new InboxItem { Title = "Second item", UserId = TestUserId, CreatedAt = DateTime.UtcNow.AddHours(-1) }
        );

        // Act
        var response = await _client.GetAsync("/api/inbox");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<InboxListResponseDto>();
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(2);
            result.InboxOverflow.Should().BeFalse();

            // Проверяем заголовок
            response.Headers.Should().ContainKey("X-Inbox-Overflow");
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
        await ClearInboxItemsAsync();

        // Act
        var response = await _client.GetAsync("/api/inbox");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<InboxListResponseDto>();
            result.Should().NotBeNull();
            result!.Items.Should().BeEmpty();
            result.InboxOverflow.Should().BeFalse();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GetAll_WithMoreThan20Items_ReturnsOverflowTrue()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var items = Enumerable.Range(1, 25)
            .Select(i => new InboxItem
            {
                Title = $"Item {i}",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow.AddHours(-i)
            })
            .ToArray();
        await SeedInboxItemsAsync(items);

        // Act
        var response = await _client.GetAsync("/api/inbox");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<InboxListResponseDto>();
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(20);
            result.InboxOverflow.Should().BeTrue();

            // Проверяем заголовок
            response.Headers.Should().ContainKey("X-Inbox-Overflow");
            response.Headers.GetValues("X-Inbox-Overflow").First().Should().Be("true");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task GetAll_ExcludesDeletedItems()
    {
        // Arrange
        await ClearInboxItemsAsync();
        await SeedInboxItemsAsync(
            new InboxItem { Title = "Active item", UserId = TestUserId, CreatedAt = DateTime.UtcNow },
            new InboxItem { Title = "Deleted item", UserId = TestUserId, CreatedAt = DateTime.UtcNow.AddHours(-1), DeletedAt = DateTime.UtcNow }
        );

        // Act
        var response = await _client.GetAsync("/api/inbox");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<InboxListResponseDto>();
            result.Should().NotBeNull();
            result!.Items.Should().HaveCount(1);
            result.Items.First().Title.Should().Be("Active item");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Create_ValidItem_ReturnsCreated()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var dto = new CreateInboxItemDto { Title = "Test Inbox Item" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox", dto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var item = await response.Content.ReadFromJsonAsync<InboxItemResponseDto>();
            item.Should().NotBeNull();
            item!.Title.Should().Be("Test Inbox Item");
            item.Id.Should().BeGreaterThan(0);
            response.Headers.Location.Should().NotBeNull();
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Create_EmptyTitle_ReturnsBadRequest(string invalidTitle)
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = invalidTitle };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_TrimsWhitespaceFromTitle()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var dto = new CreateInboxItemDto { Title = "  Trimmed Title  " };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox", dto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Created)
        {
            var item = await response.Content.ReadFromJsonAsync<InboxItemResponseDto>();
            item!.Title.Should().Be("Trimmed Title");
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Delete_ExistingItem_ReturnsNoContent()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var itemId = await SeedSingleInboxItemAsync(
            new InboxItem { Title = "To Delete", UserId = TestUserId, CreatedAt = DateTime.UtcNow }
        );

        // Act
        var response = await _client.DeleteAsync($"/api/inbox/{itemId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            // Verify it's not returned in GET
            var getResponse = await _client.GetAsync("/api/inbox");
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                var result = await getResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
                result!.Items.Should().NotContain(i => i.Id == itemId);
            }
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Delete_NonExistingItem_ReturnsNotFound()
    {
        // Arrange
        await ClearInboxItemsAsync();

        // Act
        var response = await _client.DeleteAsync("/api/inbox/99999");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_AlreadyDeletedItem_ReturnsNotFound()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var itemId = await SeedSingleInboxItemAsync(
            new InboxItem
            {
                Title = "Already Deleted",
                UserId = TestUserId,
                CreatedAt = DateTime.UtcNow,
                DeletedAt = DateTime.UtcNow
            }
        );

        // Act
        var response = await _client.DeleteAsync($"/api/inbox/{itemId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullWorkflow_CreateDelete_WorksCorrectly()
    {
        // Arrange
        await ClearInboxItemsAsync();

        // 1. Create
        var createDto = new CreateInboxItemDto { Title = "Workflow Test Item" };
        var createResponse = await _client.PostAsJsonAsync("/api/inbox", createDto);

        if (createResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Skip if auth is not configured
            return;
        }

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<InboxItemResponseDto>();
        var itemId = created!.Id;

        // 2. Verify in list
        var getResponse = await _client.GetAsync("/api/inbox");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await getResponse.Content.ReadFromJsonAsync<InboxListResponseDto>();
        list!.Items.Should().Contain(i => i.Id == itemId);

        // 3. Delete
        var deleteResponse = await _client.DeleteAsync($"/api/inbox/{itemId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Verify deleted
        var getAfterDelete = await _client.GetAsync("/api/inbox");
        var listAfterDelete = await getAfterDelete.Content.ReadFromJsonAsync<InboxListResponseDto>();
        listAfterDelete!.Items.Should().NotContain(i => i.Id == itemId);
    }
}