using AppApi.Models.DTOs;
using Common.Enums;
using FluentAssertions;
using System.Text.Json;

namespace AppApi.Tests.Models.DTOs;

public class InboxClassificationDtosTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ClassifyInboxItemDto_ValidTaskData_ConvertsToCreateTaskDto()
    {
        // Arrange - Обновлены ключи: entityType, entityData
        var json = """
        {
            "entityType": "task",
            "mode": "convert",
            "entityData": {
                "title": "Test Task",
                "content": "Task description"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createTaskDto = dto!.ToCreateTaskDto();

        // Assert
        dto.EntityType.Should().Be("task");
        dto.Mode.Should().Be("convert");
        createTaskDto.Title.Should().Be("Test Task");
        createTaskDto.Content.Should().Be("Task description");
    }

    [Fact]
    public void ClassifyInboxItemDto_ValidProjectData_ConvertsToCreateProjectDto()
    {
        // Arrange - Обновлены ключи: entityType, entityData
        var json = """
        {
            "entityType": "project",
            "mode": "create",
            "entityData": {
                "title": "New Project",
                "description": "Project description"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createProjectDto = dto!.ToCreateProjectDto();

        // Assert
        dto.EntityType.Should().Be("project");
        dto.Mode.Should().Be("create");
        createProjectDto.Name.Should().Be("New Project");
        createProjectDto.Description.Should().Be("Project description");
    }

    [Fact]
    public void ClassifyInboxItemDto_ValidRoutineData_ConvertsToCreateRoutineDto()
    {
        // Arrange - Обновлены ключи: entityType, entityData
        var json = """
        {
            "entityType": "routine",
            "entityData": {
                "title": "Morning Exercise",
                "frequency": "daily"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createRoutineDto = dto!.ToCreateRoutineDto();

        // Assert
        dto.EntityType.Should().Be("routine");
        // Проверка значения по умолчанию
        dto.Mode.Should().Be("convert");
        createRoutineDto.Name.Should().Be("Morning Exercise");
        createRoutineDto.Frequency.Should().Be(RoutineFrequency.Daily);
    }

    [Fact]
    public void ToCreateTaskDto_NullTitle_ThrowsArgumentException()
    {
        // Arrange - Обновлены ключи
        var json = """
        {
            "entityType": "task",
            "entityData": {
                "content": "No title here"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        Action act = () => dto!.ToCreateTaskDto();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title is required*");
    }

    [Fact]
    public void ToCreateRoutineDto_MissingFrequency_ThrowsArgumentException()
    {
        // Arrange - Обновлены ключи
        var json = """
        {
            "entityType": "routine",
            "entityData": {
                "title": "No frequency"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        Action act = () => dto!.ToCreateRoutineDto();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Frequency is required*");
    }

    [Fact]
    public void ClassifyInboxItemDto_DefaultMode_IsConvert()
    {
        // Arrange
        var json = """
        {
            "entityType": "task",
            "entityData": { "title": "Test" }
        }
        """;

        // Act
        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Assert
        dto!.Mode.Should().Be("convert");
    }
}