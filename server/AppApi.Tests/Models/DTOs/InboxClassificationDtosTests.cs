// AppApi.Tests/Models/DTOs/InboxClassificationDtosTests.cs
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
        // Arrange
        var json = """
        {
            "targetType": "task",
            "data": {
                "title": "Test Task",
                "content": "Task description"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createTaskDto = dto!.ToCreateTaskDto();

        // Assert
        createTaskDto.Title.Should().Be("Test Task");
        createTaskDto.Content.Should().Be("Task description");
    }

    [Fact]
    public void ClassifyInboxItemDto_ValidProjectData_ConvertsToCreateProjectDto()
    {
        // Arrange
        var json = """
        {
            "targetType": "project",
            "data": {
                "title": "New Project",
                "description": "Project description"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createProjectDto = dto!.ToCreateProjectDto();

        // Assert
        createProjectDto.Name.Should().Be("New Project");
        createProjectDto.Description.Should().Be("Project description");
    }

    [Fact]
    public void ClassifyInboxItemDto_ValidRoutineData_ConvertsToCreateRoutineDto()
    {
        // Arrange
        var json = """
        {
            "targetType": "routine",
            "data": {
                "title": "Morning Exercise",
                "frequency": "daily"
            }
        }
        """;

        var dto = JsonSerializer.Deserialize<ClassifyInboxItemDto>(json, JsonOptions);

        // Act
        var createRoutineDto = dto!.ToCreateRoutineDto();

        // Assert
        createRoutineDto.Name.Should().Be("Morning Exercise");
        createRoutineDto.Frequency.Should().Be(RoutineFrequency.Daily);
    }

    [Fact]
    public void ToCreateTaskDto_NullTitle_ThrowsArgumentException()
    {
        // Arrange
        var json = """
        {
            "targetType": "task",
            "data": {
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
        // Arrange
        var json = """
        {
            "targetType": "routine",
            "data": {
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
}