using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectService> _serviceMock;
    private readonly ProjectsController _controller;
    private const string UserId = "github-1";

    public ProjectsControllerTests()
    {
        _serviceMock = new Mock<IProjectService>();
        var loggerMock = new Mock<ILogger<ProjectsController>>();
        _controller = new ProjectsController(_serviceMock.Object, loggerMock.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, UserId)
    }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost/api/projects/1");
        _controller.Url = urlHelperMock.Object;
    }

    [Fact]
    public async Task Create_InvalidInput_ReturnsBadRequest()
    {
        var dto = new CreateProjectDto { Name = "" };

        _controller.ModelState.AddModelError("Name", "Required");

        var result = await _controller.Create(dto);

        _serviceMock.Verify(s => s.CreateProjectAsync(dto, UserId), Times.Never);
    }

    [Fact]
    public async Task Update_NonExistent_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.UpdateProjectAsync(99, It.IsAny<UpdateProjectDto>(), UserId))
            .ReturnsAsync((ProjectResponseDto?)null);

        var result = await _controller.Update(99, new UpdateProjectDto { Name = "X" });

        result.Should().BeOfType<NotFoundResult>();
    }
}