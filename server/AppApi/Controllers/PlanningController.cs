using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Common.Enums;

namespace AppApi.Controllers;

[ApiController]
[Route("api/planning")]
[Authorize(AuthenticationSchemes = "GitHub")]
public class PlanningController : ControllerBase
{
    private readonly IProjectService _projectService;

    public PlanningController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    private string GetCurrentUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException();

    /// <summary>
    /// Проекты с их задачами для отображения в Планировании (заглушка)
    /// </summary>
    [HttpGet("projects")]
    [ProducesResponseType(typeof(PlanningResponseDto), StatusCodes.Status200OK)]
    public IActionResult GetProjects()
    {
        var response = new PlanningResponseDto
        {
            Projects = new[]
            {
                new PlanningProjectDto
                {
                    Id = 1,
                    Name = "Текучка",
                    Description = null,
                    Tasks = new[]
                    {
                        new PlanningTaskDto
                        {
                            Id = 1,
                            Title = "Задача 1",
                            Status = TasksStatus.Available,
                            Selected = false
                        },
                        new PlanningTaskDto
                        {
                            Id = 2,
                            Title = "Задача 2",
                            Status = TasksStatus.Available,
                            Selected = true
                        }
                    }
                },
                new PlanningProjectDto
                {
                    Id = 2,
                    Name = "Проект X",
                    Description = "Описание",
                    Tasks = new[]
                    {
                        new PlanningTaskDto
                        {
                            Id = 3,
                            Title = "Тестовая задача",
                            Status = TasksStatus.InProgress,
                            Selected = true
                        }
                    }
                }
            },
            TotalProjects = 2
        };

        return Ok(response);
    }
}