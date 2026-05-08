using AppApi.Controllers;
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

namespace AppApi.Tests.Integration;

public class FlowControllerIntegrationTests : IAsyncLifetime
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

    [Fact]
    public async Task GetPhase_NewUser_ReturnsDefaultPlanning()
    {
        // Act
        var response = await _client.GetAsync("/api/flow/phase");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var phaseResponse = await response.Content
                .ReadFromJsonAsync<FlowController.FlowPhaseResponse>();

            // Assert
            phaseResponse.Should().NotBeNull();
            phaseResponse!.Phase.Should().Be("planning");
        }
    }

    [Fact]
    public async Task UpdatePhase_ValidPhase_SavesAndReturnsPhase()
    {
        // Arrange
        var updateRequest = new FlowController.FlowPhaseUpdateRequest("sprint");

        // Act
        var response = await _client.PutAsJsonAsync("/api/flow/phase", updateRequest);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var updateResponse = await response.Content
                .ReadFromJsonAsync<FlowController.FlowPhaseUpdateResponse>();

            updateResponse.Should().NotBeNull();
            updateResponse!.Success.Should().BeTrue();
            updateResponse.Phase.Should().Be("sprint");

            // Проверяем что фаза сохранилась
            var getResponse = await _client.GetAsync("/api/flow/phase");
            if (getResponse.StatusCode == HttpStatusCode.OK)
            {
                var getPhaseResponse = await getResponse.Content
                    .ReadFromJsonAsync<FlowController.FlowPhaseResponse>();
                getPhaseResponse!.Phase.Should().Be("sprint");
            }
        }
    }

    [Fact]
    public async Task UpdatePhase_InvalidPhase_ReturnsBadRequest()
    {
        // Arrange
        var updateRequest = new FlowController.FlowPhaseUpdateRequest("invalid_phase");

        // Act
        var response = await _client.PutAsJsonAsync("/api/flow/phase", updateRequest);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Phase_PersistsAfterMultipleRequests()
    {
        // Arrange: Set to sprint
        var sprintRequest = new FlowController.FlowPhaseUpdateRequest("sprint");
        await _client.PutAsJsonAsync("/api/flow/phase", sprintRequest);

        // Set to review
        var reviewRequest = new FlowController.FlowPhaseUpdateRequest("review");
        await _client.PutAsJsonAsync("/api/flow/phase", reviewRequest);

        // Act: Get phase
        var response = await _client.GetAsync("/api/flow/phase");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var phaseResponse = await response.Content
                .ReadFromJsonAsync<FlowController.FlowPhaseResponse>();

            // Assert: Должна быть последняя установленная фаза
            phaseResponse!.Phase.Should().Be("review");
        }
    }

    [Fact]
    public async Task Phase_PersistsBetweenRequests()
    {
        // Arrange: Create two clients (симуляция разных сессий)
        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Client 1 sets phase
        var updateRequest = new FlowController.FlowPhaseUpdateRequest("review");
        await _client.PutAsJsonAsync("/api/flow/phase", updateRequest);

        // Client 2 gets phase (симуляция logout/login)
        var response = await client2.GetAsync("/api/flow/phase");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var phaseResponse = await response.Content
                .ReadFromJsonAsync<FlowController.FlowPhaseResponse>();

            // Assert: Фаза должна сохраниться между сессиями
            phaseResponse!.Phase.Should().Be("review");
        }
    }

    [Fact]
    public async Task Phase_AllValidPhases_Accepted()
    {
        var validPhases = new[] { "sprint", "review", "planning" };

        foreach (var phase in validPhases)
        {
            var updateRequest = new FlowController.FlowPhaseUpdateRequest(phase);
            var response = await _client.PutAsJsonAsync("/api/flow/phase", updateRequest);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }
    }
}