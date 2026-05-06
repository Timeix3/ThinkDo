using AppApi.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class FlowControllerTests
{
    private static FlowController CreateController(string userId)
    {
        return new FlowController
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
    public void GetPhase_ForFirstTimeUser_ReturnsPlanning()
    {
        var controller = CreateController($"first-user-{Guid.NewGuid()}");

        var result = controller.GetPhase();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<FlowController.FlowPhaseResponse>().Subject;
        response.Phase.Should().Be("planning");
    }

    [Fact]
    public void UpdatePhase_WithValidPhase_ReturnsOkAndSavesPhase()
    {
        var userId = $"test-user-{Guid.NewGuid()}";
        var controller = CreateController(userId);

        var updateResult = controller.UpdatePhase(new FlowController.FlowPhaseUpdateRequest("review"));

        var ok = updateResult.Should().BeOfType<OkObjectResult>().Subject;
        var updateResponse = ok.Value.Should().BeOfType<FlowController.FlowPhaseUpdateResponse>().Subject;
        updateResponse.Success.Should().BeTrue();
        updateResponse.Phase.Should().Be("review");

        var getResult = controller.GetPhase();
        var getOk = getResult.Should().BeOfType<OkObjectResult>().Subject;
        var getResponse = getOk.Value.Should().BeOfType<FlowController.FlowPhaseResponse>().Subject;
        getResponse.Phase.Should().Be("review");
    }

    [Fact]
    public void UpdatePhase_WithInvalidPhase_ReturnsBadRequest()
    {
        var controller = CreateController($"invalid-user-{Guid.NewGuid()}");

        var result = controller.UpdatePhase(new FlowController.FlowPhaseUpdateRequest("invalid"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
