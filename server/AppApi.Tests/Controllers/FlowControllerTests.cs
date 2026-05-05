using AppApi.Controllers;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class FlowControllerTests
{
    private static FlowController CreateController(string userId, Mock<IFlowPhaseService> serviceMock)
    {
        return new FlowController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId)
                    }, "Test"))
                }
            }
        };
    }

    [Fact]
    public async Task GetPhase_ForFirstTimeUser_ReturnsPlanning()
    {
        var serviceMock = new Mock<IFlowPhaseService>();
        serviceMock.Setup(x => x.GetPhaseAsync(It.IsAny<string>())).ReturnsAsync("planning");
        var controller = CreateController($"first-user-{Guid.NewGuid()}", serviceMock);

        var result = await controller.GetPhase();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<FlowController.FlowPhaseResponse>().Subject;
        response.Phase.Should().Be("planning");
    }

    [Fact]
    public async Task UpdatePhase_WithValidPhase_ReturnsOk()
    {
        var serviceMock = new Mock<IFlowPhaseService>();
        serviceMock.Setup(x => x.SetPhaseAsync(It.IsAny<string>(), "review")).ReturnsAsync("review");

        var controller = CreateController($"test-user-{Guid.NewGuid()}", serviceMock);

        var updateResult = await controller.UpdatePhase(new FlowController.FlowPhaseUpdateRequest("review"));

        var ok = updateResult.Should().BeOfType<OkObjectResult>().Subject;
        var updateResponse = ok.Value.Should().BeOfType<FlowController.FlowPhaseUpdateResponse>().Subject;
        updateResponse.Success.Should().BeTrue();
        updateResponse.Phase.Should().Be("review");
    }

    [Fact]
    public async Task UpdatePhase_WithInvalidPhase_ReturnsBadRequest()
    {
        var serviceMock = new Mock<IFlowPhaseService>();
        serviceMock.Setup(x => x.SetPhaseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Invalid phase"));

        var controller = CreateController($"invalid-user-{Guid.NewGuid()}", serviceMock);

        var result = await controller.UpdatePhase(new FlowController.FlowPhaseUpdateRequest("invalid"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
