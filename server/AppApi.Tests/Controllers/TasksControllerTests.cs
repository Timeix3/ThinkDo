using AppApi.Controllers;
using AppApi.Models.DTOs;
using AppApi.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace AppApi.Tests.Controllers;

public class TasksControllerTests
{
    private readonly Mock<ITaskService> _serviceMock;
    private readonly Mock<ILogger<TasksController>> _loggerMock;
    private readonly TasksController _controller;
    private const string TestUserId = "test-user-123";

    public TasksControllerTests()
    {
        _serviceMock = new Mock<ITaskService>();
        _loggerMock = new Mock<ILogger<TasksController>>();
        _controller = new TasksController(_serviceMock.Object, _loggerMock.Object);

        // Setup authenticated user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, TestUserId),
            new Claim(ClaimTypes.Name, "testuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTasks()
    {
        var tasks = new List<TaskResponseDto>
        {
            new() { Id = 1, Title = "Task 1", Content = "Content 1", CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Title = "Task 2", Content = "Content 2", CreatedAt = DateTime.UtcNow }
        };
        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId)).ReturnsAsync(tasks);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponseDto>>().Subject;
        returnedTasks.Should().HaveCount(2);
        _serviceMock.Verify(s => s.GetAllTasksAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId)).ReturnsAsync(new List<TaskResponseDto>());

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskResponseDto>>().Subject;
        returnedTasks.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ExistingTask_ReturnsOkWithTask()
    {
        var task = new TaskResponseDto { Id = 1, Title = "Task 1", Content = "Content 1", CreatedAt = DateTime.UtcNow };
        _serviceMock.Setup(s => s.GetTaskByIdAsync(1, TestUserId)).ReturnsAsync(task);

        var result = await _controller.GetById(1);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTask = okResult.Value.Should().BeOfType<TaskResponseDto>().Subject;
        returnedTask.Id.Should().Be(1);
        returnedTask.Title.Should().Be("Task 1");
    }

    [Fact]
    public async Task GetById_NonExistingTask_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.GetTaskByIdAsync(999, TestUserId)).ReturnsAsync((TaskResponseDto?)null);

        var result = await _controller.GetById(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_ValidDto_ReturnsCreatedAtAction()
    {
        var dto = new CreateTaskDto { Title = "New Task", Content = "New Content" };
        var createdTask = new TaskResponseDto { Id = 1, Title = "New Task", Content = "New Content", CreatedAt = DateTime.UtcNow };
        _serviceMock.Setup(s => s.CreateTaskAsync(dto, TestUserId)).ReturnsAsync(createdTask);

        var result = await _controller.Create(dto);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetById");
        var returnedTask = createdResult.Value.Should().BeOfType<TaskResponseDto>().Subject;
        returnedTask.Title.Should().Be("New Task");
    }

    [Fact]
    public async Task Update_ExistingTask_ReturnsOkWithUpdatedTask()
    {
        var dto = new UpdateTaskDto { Title = "Updated", Content = "Updated Content" };
        var updatedTask = new TaskResponseDto { Id = 1, Title = "Updated", Content = "Updated Content", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _serviceMock.Setup(s => s.UpdateTaskAsync(1, dto, TestUserId)).ReturnsAsync(updatedTask);

        var result = await _controller.Update(1, dto);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTask = okResult.Value.Should().BeOfType<TaskResponseDto>().Subject;
        returnedTask.Title.Should().Be("Updated");
    }

    [Fact]
    public async Task Update_NonExistingTask_ReturnsNotFound()
    {
        var dto = new UpdateTaskDto { Title = "Updated", Content = "Updated Content" };
        _serviceMock.Setup(s => s.UpdateTaskAsync(999, dto, TestUserId)).ReturnsAsync((TaskResponseDto?)null);

        var result = await _controller.Update(999, dto);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Delete_ExistingTask_ReturnsNoContent()
    {
        _serviceMock.Setup(s => s.DeleteTaskAsync(1, TestUserId)).ReturnsAsync(true);

        var result = await _controller.Delete(1);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_NonExistingTask_ReturnsNotFound()
    {
        _serviceMock.Setup(s => s.DeleteTaskAsync(999, TestUserId)).ReturnsAsync(false);

        var result = await _controller.Delete(999);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAll_UserIdExtractedFromClaims()
    {
        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId)).ReturnsAsync(new List<TaskResponseDto>());

        await _controller.GetAll();

        _serviceMock.Verify(s => s.GetAllTasksAsync(TestUserId), Times.Once);
    }
}
