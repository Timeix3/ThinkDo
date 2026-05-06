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
    [Required(ErrorMessage = "EntityType is required")]
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Режим: convert (с удалением) или create (без удаления). По умолчанию convert.
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "convert";

    /// <summary>
    /// Данные для создания целевой сущности
    /// </summary>
    [Required(ErrorMessage = "EntityData is required")]
    [JsonPropertyName("entityData")]
    public JsonElement EntityData { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Преобразование JSON-данных в DTO создания задачи
    /// </summary>
    public CreateTaskDto ToCreateTaskDto()
    {
        var rawJson = EntityData.GetRawText();
        var data = JsonSerializer.Deserialize<CreateTaskFromInboxDto>(rawJson, JsonOptions);

        if (data is null || string.IsNullOrWhiteSpace(data.Title))
        {
            throw new ArgumentException("Title is required for task");
        }

        return new CreateTaskDto
        {
            Title = data.Title,
            Content = data.Content,
            ProjectId = data.ProjectId
        };
    }

    /// <summary>
    /// Преобразование JSON-данных в DTO создания проекта
    /// </summary>
    public CreateProjectDto ToCreateProjectDto()
    {
        var rawJson = EntityData.GetRawText();
        var data = JsonSerializer.Deserialize<CreateProjectFromInboxDto>(rawJson, JsonOptions);

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
    /// Преобразование JSON-данных в DTO создания рутины
    /// </summary>
    public CreateRoutineDto ToCreateRoutineDto()
    {
        var rawJson = EntityData.GetRawText();
        var data = JsonSerializer.Deserialize<CreateRoutineFromInboxDto>(rawJson, JsonOptions);

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
/// Вспомогательный класс для десериализации задачи из полиморфного поля
/// </summary>
public class CreateTaskFromInboxDto
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int? ProjectId { get; set; }
}

/// <summary>
/// Вспомогательный класс для десериализации проекта из полиморфного поля
/// </summary>
public class CreateProjectFromInboxDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Вспомогательный класс для десериализации рутины из полиморфного поля
/// </summary>
public class CreateRoutineFromInboxDto
{
    public string? Title { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RoutineFrequency? Frequency { get; set; }
}

/// <summary>
/// Финальный ответ на классификацию согласно
/// </summary>
public class ClassifyResponseDto
{
    /// <summary>
    /// Флаг успешности операции
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// ID созданной сущности (задачи/проекта/рутины)
    /// </summary>
    public int CreatedEntityId { get; set; }

    /// <summary>
    /// Был ли удален исходный элемент инбокса
    /// </summary>
    public bool InboxDeleted { get; set; }
}