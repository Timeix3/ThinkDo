using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AppApi.Models.DTOs;
using Common.Data;
using Common.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace AppApi.Tests.Integration;

public class InboxClassificationIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("testdb_inbox")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string TestUserId = "github-test-user-123";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // 1. Применяем миграции ДО старта приложения (фикс ошибки "projects already exists")
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        using (var setupContext = new AppDbContext(options))
        {
            await setupContext.Database.MigrateAsync();
        }

        // 2. Настройка фабрики
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureTestServices(services =>
                {
                    // Замена БД
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(_postgres.GetConnectionString()));

                    // === РЕШЕНИЕ ОШИБКИ "Scheme already exists" ===
                    // Вместо AddAuthentication мы просто находим существующую схему и меняем ей Handler
                    services.PostConfigure<AuthenticationOptions>(options =>
                    {
                        var scheme = options.Schemes.FirstOrDefault(s => s.Name == "GitHub");
                        if (scheme != null)
                        {
                            // Подменяем тип обработчика на наш тестовый
                            scheme.HandlerType = typeof(TestAuthHandler);
                        }
                    });

                    // Регистрируем сам обработчик в DI
                    services.AddTransient<TestAuthHandler>();
                });
            });

        _client = _factory.CreateClient();
        // Используем имя схемы GitHub в заголовке
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("GitHub");
    }

    public async Task DisposeAsync()
    {
        // HttpClient удаляем синхронно (фикс ошибки DisposeAsync)
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<int> SeedInboxItemAsync(string title)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = new InboxItem { Title = title, UserId = TestUserId, CreatedAt = DateTime.UtcNow };
        db.InboxItems.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.InboxItems.RemoveRange(db.InboxItems);
        db.Tasks.RemoveRange(db.Tasks);
        await db.SaveChangesAsync();
    }

    // --- ТЕСТЫ ---

    [Fact]
    public async Task Classify_ToTask_ModeConvert_DeletesInbox()
    {
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Test Task");
        var request = new { entityType = "task", mode = "convert", entityData = new { title = "Real Task" } };

        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ClassifyResponseDto>();
        result!.InboxDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Classify_ModeCreate_KeepsInboxActive()
    {
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Template");
        var request = new { entityType = "task", mode = "create", entityData = new { title = "New One" } };

        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ClassifyResponseDto>();
        result!.InboxDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Classify_CreationFails_InboxNotDeleted()
    {
        await ClearDatabaseAsync();
        var inboxId = await SeedInboxItemAsync("Safe record");
        // Пустой заголовок вызовет BadRequest
        var request = new { entityType = "task", mode = "convert", entityData = new { title = "" } };

        var response = await _client.PostAsJsonAsync($"/api/inbox/{inboxId}/classify", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = await db.InboxItems.FindAsync(inboxId);
        item.Should().NotBeNull();
        item!.DeletedAt.Should().BeNull();
    }

    // --- ТЕСТОВЫЙ ОБРАБОТЧИК АУТЕНТИФИКАЦИИ ---
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) };
            var identity = new ClaimsIdentity(claims, "GitHub");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "GitHub");
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}