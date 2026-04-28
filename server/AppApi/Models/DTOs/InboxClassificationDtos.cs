// AppApi/Models/DTOs/InboxClassificationDtos.cs
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Enums;

namespace AppApi.Models.DTOs;

/// <summary>
/// Запрос на классификацию записи инбокса
/// </summary>
public class ClassifyInboxItemDto
{
    /// <summary>
    /// Тип целевой сущности: task, project, routine
    /// </summary>
    [Required(ErrorMessage = "TargetType is required")]
    [JsonPropertyName("targetType")]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// Данные для создания целевой сущности (полиморфные)
    /// </summary>
    [Required(ErrorMessage = "Data is required")]
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Получить DTO для создания задачи
    /// </summary>
    public CreateTaskDto ToCreateTaskDto()
    {
        var data = JsonSerializer.Deserialize<CreateTaskFromInboxDto>(
            Data.GetRawText(), JsonOptions);

        if (data is null || string.IsNullOrWhiteSpace(data.Title))
        {
            throw new ArgumentException("Title is required for task");
        }

        return new CreateTaskDto
        {
            Title = data.Title,
            Content = data.Content
        };
    }

    /// <summary>
    /// Получить DTO для создания проекта
    /// </summary>
    public CreateProjectDto ToCreateProjectDto()
    {
        var data = JsonSerializer.Deserialize<CreateProjectFromInboxDto>(
            Data.GetRawText(), JsonOptions);

        if (data is null || string.IsNullOrWhiteSpace(data.Title))
        {
            throw new ArgumentException("Title/Name is required for project");
        }

        return new CreateProjectDto
        {
            Name = data.Title,
            Description = data.Description
        };
    }

    /// <summary>
    /// Получить DTO для создания рутины
    /// </summary>
    public CreateRoutineDto ToCreateRoutineDto()
    {
        var data = JsonSerializer.Deserialize<CreateRoutineFromInboxDto>(
            Data.GetRawText(), JsonOptions);

        if (data is null || string.IsNullOrWhiteSpace(data.Title))
        {
            throw new ArgumentException("Title/Name is required for routine");
        }

        if (data.Frequency is null)
        {
            throw new ArgumentException("Frequency is required for routine");
        }

        return new CreateRoutineDto
        {
            Name = data.Title,
            Frequency = data.Frequency.Value
        };
    }
}

/// <summary>
/// DTO для данных задачи из инбокса
/// </summary>
public class CreateTaskFromInboxDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? ProjectId { get; set; }
}

/// <summary>
/// DTO для данных проекта из инбокса
/// </summary>
public class CreateProjectFromInboxDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// DTO для данных рутины из инбокса
/// </summary>
public class CreateRoutineFromInboxDto
{
    public string? Title { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutineFrequency? Frequency { get; set; }
}

/// <summary>
/// Ответ на классификацию
/// </summary>
public class ClassifyInboxItemResponseDto
{
    public int Id { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Frequency { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }
}