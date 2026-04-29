using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class SprintControllerTests
{
    [Fact]
    public async Task GetSprintTasks_ReturnsOk()
    {
        var serviceMock = new Mock<ITaskService>();
        serviceMock.Setup(s => s.GetSprintTasksAsync("test-user-123"))
            .ReturnsAsync(new[] { new TaskResponseDto { Id = 1, Title = "T" } });

        var controller = new SprintController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "test-user-123")
                    }, "Test"))
                }
            }
        };

        var result = await controller.GetSprintTasks();
        result.Should().BeOfType<OkObjectResult>();
    }
}