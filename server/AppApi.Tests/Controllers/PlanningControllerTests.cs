using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class PlanningControllerTests
{
    [Fact]
    public async Task GetProjects_ReturnsOk()
    {
        var serviceMock = new Mock<IProjectService>();
        serviceMock.Setup(s => s.GetPlanningProjectsAsync("test-user-123"))
            .ReturnsAsync(new[] { new PlanningProjectResponseDto { Id = 1, Name = "P" } });

        var controller = new PlanningController(serviceMock.Object)
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

        var result = await controller.GetProjects();
        result.Should().BeOfType<OkObjectResult>();
    }
}