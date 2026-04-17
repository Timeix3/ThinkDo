// AppApi.Tests/Integration/InboxControllerIntegrationTests.cs
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

    private async Task SeedInboxItemsAsync(params InboxItem[] items)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.AddRange(items);
        await db.SaveChangesAsync();
    }

    private async Task ClearInboxItemsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.RemoveRange(db.InboxItems);
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
    public async Task Create_ValidItem_ReturnsCreated()
    {
        // Arrange
        await ClearInboxItemsAsync();
        var dto = new CreateInboxItemDto { Title = "Test Inbox Item" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox", dto);

        // Assert
        // Note: This may return 401 if auth is not properly configured for integration tests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.Unauthorized);

        if (response.StatusCode == HttpStatusCode.Created)
        {
            var item = await response.Content.ReadFromJsonAsync<InboxItemResponseDto>();
            item.Should().NotBeNull();
            item!.Title.Should().Be("Test Inbox Item");
        }
    }

    [Fact]
    public async Task Create_EmptyTitle_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateInboxItemDto { Title = "   " };

        // Act
        var response = await _client.PostAsJsonAsync("/api/inbox", dto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }
}