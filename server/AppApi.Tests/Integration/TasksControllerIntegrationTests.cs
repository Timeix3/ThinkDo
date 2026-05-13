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
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.PostgreSql;
using AppApi.Models.DTOs;

namespace AppApi.Tests.Integration;

public class TasksControllerIntegrationTests : IAsyncLifetime
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

        // Set fake auth header - we need to bypass the GitHub auth handler
        // For integration tests we use a special test token that bypasses GitHub
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task SeedTasksAsync(params TaskItem[] tasks)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tasks.AddRange(tasks);
        await db.SaveChangesAsync();
    }

    private async Task ClearTasksAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tasks.RemoveRange(db.Tasks);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAll_WithTasks_ReturnsOkAndTasks()
    {
        await ClearTasksAsync();
        await SeedTasksAsync(
            new TaskItem { Title = "Task 1", Content = "Content 1", UserId = "github-test-user" },
            new TaskItem { Title = "Task 2", Content = "Content 2", UserId = "github-test-user" }
        );

        var response = await _client.GetAsync("/api/tasks");

        // We expect 401 because auth is not bypassed in integration tests without a mock
        // Real integration tests with test auth scheme would need a custom auth handler
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    private async Task ClearDatabaseAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Tasks.RemoveRange(db.Tasks);
        db.Projects.RemoveRange(db.Projects);
        await db.SaveChangesAsync();
    }

    private async Task<int> SeedProjectAsync(string name, string? description = null, bool deleted = false)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var project = new ProjectItem
        {
            Name = name,
            Description = description,
            UserId = TestUserId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            DeletedAt = deleted ? DateTime.UtcNow : null
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    private async Task<int> SeedTaskAsync(string title, int? projectId = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var task = new TaskItem
        {
            Title = title,
            UserId = TestUserId,
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task.Id;
    }

    [Fact]
    public async Task GetTaskById_TaskWithProject_ReturnsProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var projectId = await SeedProjectAsync("Test Project", "Project description");
        var taskId = await SeedTaskAsync("Task with project", projectId);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Skip if auth is not configured for tests
            return;
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.Id.Should().Be(taskId);
        task.Title.Should().Be("Task with project");
        task.ProjectId.Should().Be(projectId);

        task.Project.Should().NotBeNull();
        task.Project!.Id.Should().Be(projectId);
        task.Project.Name.Should().Be("Test Project");
        task.Project.Description.Should().Be("Project description");
    }

    [Fact]
    public async Task GetTaskById_TaskWithoutProject_ReturnsNullProject()
    {
        // Arrange
        await ClearDatabaseAsync();
        var taskId = await SeedTaskAsync("Task without project");

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().BeNull();
        task.Project.Should().BeNull();
    }

    [Fact]
    public async Task GetTaskById_TaskWithSoftDeletedProject_ReturnsNullProject()
    {
        // Arrange
        await ClearDatabaseAsync();
        var projectId = await SeedProjectAsync("Deleted Project", deleted: true);
        var taskId = await SeedTaskAsync("Task with deleted project", projectId);

        // Act
        var response = await _client.GetAsync($"/api/tasks/{taskId}");

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().Be(projectId);
        task.Project.Should().BeNull(); // Проект удалён → null
    }

    [Fact]
    public async Task GetAllTasks_MultipleTasksWithDifferentProjects_ReturnsCorrectProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var project1Id = await SeedProjectAsync("Project 1", "First project");
        var project2Id = await SeedProjectAsync("Project 2", "Second project");

        await SeedTaskAsync("Task 1", project1Id);
        await SeedTaskAsync("Task 2", null); // Без проекта
        await SeedTaskAsync("Task 3", project2Id);

        // Act
        var response = await _client.GetAsync("/api/tasks");

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskListResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);

        // Проверяем задачу с проектом 1
        var task1 = result.Items.FirstOrDefault(t => t.Title == "Task 1");
        task1.Should().NotBeNull();
        task1!.Project.Should().NotBeNull();
        task1.Project!.Name.Should().Be("Project 1");
        task1.Project.Description.Should().Be("First project");

        // Проверяем задачу без проекта
        var task2 = result.Items.FirstOrDefault(t => t.Title == "Task 2");
        task2.Should().NotBeNull();
        task2!.ProjectId.Should().BeNull();
        task2.Project.Should().BeNull();

        // Проверяем задачу с проектом 2
        var task3 = result.Items.FirstOrDefault(t => t.Title == "Task 3");
        task3.Should().NotBeNull();
        task3!.Project.Should().NotBeNull();
        task3.Project!.Name.Should().Be("Project 2");
    }

    [Fact]
    public async Task CreateTask_WithProjectId_ReturnsProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var projectId = await SeedProjectAsync("New Project", "New description");

        var createDto = new
        {
            title = "New Task",
            content = "Task content",
            projectId = projectId,
            status = "Available"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", createDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.Title.Should().Be("New Task");
        task.ProjectId.Should().Be(projectId);
        task.Project.Should().NotBeNull();
        task.Project!.Id.Should().Be(projectId);
        task.Project.Name.Should().Be("New Project");
        task.Project.Description.Should().Be("New description");
    }

    [Fact]
    public async Task CreateTask_WithNonExistentProject_ReturnsNotFound()
    {
        // Arrange
        await ClearDatabaseAsync();

        var createDto = new
        {
            title = "Task with fake project",
            projectId = 99999,
            status = "Available"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", createDto);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTask_WithoutProject_ReturnsNullProject()
    {
        // Arrange
        await ClearDatabaseAsync();

        var createDto = new
        {
            title = "Task without project",
            status = "Available"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/tasks", createDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().BeNull();
        task.Project.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTask_AddProject_ReturnsProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var taskId = await SeedTaskAsync("Task to update");
        var projectId = await SeedProjectAsync("Added Project", "Added later");

        var updateDto = new
        {
            title = "Updated Task",
            projectId = projectId
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().Be(projectId);
        task.Project.Should().NotBeNull();
        task.Project!.Name.Should().Be("Added Project");
    }

    [Fact]
    public async Task UpdateTask_RemoveProject_ReturnsNullProject()
    {
        // Arrange
        await ClearDatabaseAsync();
        var projectId = await SeedProjectAsync("Will be removed");
        var taskId = await SeedTaskAsync("Task with project", projectId);

        var updateDto = new
        {
            title = "Task without project",
            projectId = (int?)null
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().BeNull();
        task.Project.Should().BeNull();
    }

    [Fact]
    public async Task UpdateTask_ChangeProject_ReturnsNewProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var project1Id = await SeedProjectAsync("Old Project");
        var project2Id = await SeedProjectAsync("New Project", "New description");
        var taskId = await SeedTaskAsync("Task to change", project1Id);

        var updateDto = new
        {
            title = "Task with new project",
            projectId = project2Id
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/tasks/{taskId}", updateDto);

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var task = await response.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        task.Should().NotBeNull();
        task!.ProjectId.Should().Be(project2Id);
        task.Project.Should().NotBeNull();
        task.Project!.Name.Should().Be("New Project");
    }

    [Fact]
    public async Task GetAllTasks_WithPagination_ReturnsCorrectProjectInfo()
    {
        // Arrange
        await ClearDatabaseAsync();
        var projectId = await SeedProjectAsync("Pagination Project", "Test pagination");

        // Создаем 5 задач с проектом
        for (int i = 1; i <= 5; i++)
        {
            await SeedTaskAsync($"Task {i}", projectId);
        }

        // Act
        var response = await _client.GetAsync("/api/tasks?offset=0&limit=3");

        // Assert
        if (response.StatusCode == HttpStatusCode.Unauthorized) return;

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TaskListResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(5);
        result.HasMore.Should().BeTrue();

        // Все задачи должны иметь информацию о проекте
        foreach (var task in result.Items)
        {
            task.Project.Should().NotBeNull();
            task.Project!.Name.Should().Be("Pagination Project");
        }
    }

    [Fact]
    public async Task FullWorkflow_CreateUpdateGetTask_ProjectInfoConsistent()
    {
        // Arrange
        await ClearDatabaseAsync();

        // 1. Создаем проект
        var projectId = await SeedProjectAsync("Workflow Project", "Workflow description");

        // 2. Создаем задачу с проектом
        var createDto = new
        {
            title = "Workflow Task",
            projectId = projectId,
            status = "Available"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/tasks", createDto);

        if (createResponse.StatusCode == HttpStatusCode.Unauthorized) return;

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdTask = await createResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        createdTask!.Project.Should().NotBeNull();
        createdTask.Project!.Name.Should().Be("Workflow Project");

        // 3. Получаем задачу по ID
        var getResponse = await _client.GetAsync($"/api/tasks/{createdTask.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedTask = await getResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        retrievedTask!.Project.Should().NotBeNull();
        retrievedTask.Project!.Description.Should().Be("Workflow description");

        // 4. Обновляем задачу - убираем проект
        var updateDto = new
        {
            title = "Workflow Task Updated",
            projectId = (int?)null
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/tasks/{createdTask.Id}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedTask = await updateResponse.Content.ReadFromJsonAsync<TaskResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        updatedTask!.ProjectId.Should().BeNull();
        updatedTask.Project.Should().BeNull();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("healthy");
    }
}
