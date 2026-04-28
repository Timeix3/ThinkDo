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

        // var claims = new List<Claim>
        // {
        //     new Claim(ClaimTypes.NameIdentifier, TestUserId),
        //     new Claim(ClaimTypes.Name, "testuser")
        // };

        // var identity = new ClaimsIdentity(claims, "TestAuth");
        // var principal = new ClaimsPrincipal(identity);

        // _controller.ControllerContext = new ControllerContext
        // {
        //     HttpContext = new DefaultHttpContext { User = principal }
        // };

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, TestUserId) }, "Test"));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        // Мокаем IUrlHelper для CreatedAtAction
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(x => x.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost/api/tasks/1");
        _controller.Url = urlHelperMock.Object;
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithTasks()
    {
        var tasks = new TaskListResponseDto
        {
            Items = new[]
            {
                new TaskResponseDto { Id = 1, Title = "Task 1", Content = "Content 1", CreatedAt = DateTime.UtcNow },
                new TaskResponseDto { Id = 2, Title = "Task 2", Content = "Content 2", CreatedAt = DateTime.UtcNow }
            },
            TotalCount = 2,
            PageSize = 50,
            PageNumber = 1,
            HasMore = false
        };

        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId, 0, 50)).ReturnsAsync(tasks);

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<TaskListResponseDto>().Subject;
        returnedDto.Items.Should().HaveCount(2);

        _serviceMock.Verify(s => s.GetAllTasksAsync(TestUserId, 0, 50), Times.Once);
    }

    [Fact]
    public async Task GetAll_EmptyList_ReturnsOkWithEmptyList()
    {
        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId, 0, 50)).ReturnsAsync(new TaskListResponseDto
        {
            Items = Array.Empty<TaskResponseDto>(),
            TotalCount = 0,
            PageSize = 50,
            PageNumber = 1,
            HasMore = false
        });

        var result = await _controller.GetAll();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedDto = okResult.Value.Should().BeOfType<TaskListResponseDto>().Subject;
        returnedDto.Items.Should().BeEmpty();
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
        var updatedTask = new TaskResponseDto
        {
            Id = 1,
            Title = "Updated",
            Content = "Updated Content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
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
        _serviceMock.Setup(s => s.GetAllTasksAsync(TestUserId, 0, 50)).ReturnsAsync(new TaskListResponseDto
        {
            Items = Array.Empty<TaskResponseDto>(),
            TotalCount = 0,
            PageSize = 50,
            PageNumber = 1,
            HasMore = false
        });

        await _controller.GetAll();

        _serviceMock.Verify(s => s.GetAllTasksAsync(TestUserId, 0, 50), Times.Once);
    }

    [Fact]
    public async Task Create_WithStrangerProject_ReturnsNotFound()
    {
        // Arrange: Симулируем, что сервис выбросил KeyNotFoundException (проект чужой)
        var dto = new CreateTaskDto { Title = "Hack", ProjectId = 999 };
        _serviceMock.Setup(s => s.CreateTaskAsync(dto, TestUserId))
            .ThrowsAsync(new KeyNotFoundException("Проект не найден."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_WithDeletedProject_ReturnsBadRequest()
    {
        // Arrange: Симулируем, что сервис выбросил ArgumentException (проект удален)
        var dto = new CreateTaskDto { Title = "Bad Project", ProjectId = 5 };
        _serviceMock.Setup(s => s.CreateTaskAsync(dto, TestUserId))
            .ThrowsAsync(new ArgumentException("Проект удален."));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}