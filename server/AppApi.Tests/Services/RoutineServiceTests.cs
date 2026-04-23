using AppApi.Models.DTOs;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Enums;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class RoutineServiceTests
{
    private readonly Mock<IRoutineRepository> _repositoryMock;
    private readonly RoutineService _service;
    private const string TestUserId = "test-user-123";

    public RoutineServiceTests()
    {
        _repositoryMock = new Mock<IRoutineRepository>();
        _service = new RoutineService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllRoutinesAsync_WithRoutines_ReturnsAllRoutinesAsDtos()
    {
        // Arrange
        var routines = new List<Routine>
        {
            new() { Id = 1, Name = "Morning Exercise", Frequency = RoutineFrequency.Daily, UserId = TestUserId, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Weekly Review", Frequency = RoutineFrequency.Weekly, UserId = TestUserId, CreatedAt = DateTime.UtcNow }
        };
        _repositoryMock.Setup(r => r.GetAllAsync(TestUserId)).ReturnsAsync(routines);

        // Act
        var result = await _service.GetAllRoutinesAsync(TestUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<RoutineResponseDto>();
        result.First().Frequency.Should().Be(RoutineFrequency.Daily);
        _repositoryMock.Verify(r => r.GetAllAsync(TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetRoutineByIdAsync_ExistingId_ReturnsDto()
    {
        // Arrange
        var routine = new Routine
        {
            Id = 1,
            Name = "Morning Exercise",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, TestUserId)).ReturnsAsync(routine);

        // Act
        var result = await _service.GetRoutineByIdAsync(1, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Morning Exercise");
        result.Frequency.Should().Be(RoutineFrequency.Daily);
        _repositoryMock.Verify(r => r.GetByIdAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task GetRoutineByIdAsync_NonExistingId_ReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(999, TestUserId)).ReturnsAsync((Routine?)null);

        // Act
        var result = await _service.GetRoutineByIdAsync(999, TestUserId);

        // Assert
        result.Should().BeNull();
        _repositoryMock.Verify(r => r.GetByIdAsync(999, TestUserId), Times.Once);
    }

    [Fact]
    public async Task CreateRoutineAsync_ValidDto_CallsRepositoryAndReturnsDto()
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = "  Morning Exercise  ", Frequency = RoutineFrequency.Daily };
        Routine? capturedRoutine = null;

        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Routine>()))
            .Callback<Routine>(r => capturedRoutine = r)
            .ReturnsAsync((Routine r) =>
            {
                r.Id = 1;
                r.CreatedAt = DateTime.UtcNow;
                return r;
            });

        // Act
        var result = await _service.CreateRoutineAsync(dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Morning Exercise");
        result.Frequency.Should().Be(RoutineFrequency.Daily);

        capturedRoutine.Should().NotBeNull();
        capturedRoutine!.Name.Should().Be("Morning Exercise");
        capturedRoutine.UserId.Should().Be(TestUserId);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<Routine>(
            routine => routine.Name == "Morning Exercise" && routine.UserId == TestUserId)), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task CreateRoutineAsync_EmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var dto = new CreateRoutineDto { Name = invalidName, Frequency = RoutineFrequency.Daily };

        // Act
        Func<Task> act = () => _service.CreateRoutineAsync(dto, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name cannot be empty or whitespace");

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Routine>()), Times.Never);
    }

    [Fact]
    public async Task UpdateRoutineAsync_ExistingRoutine_ReturnsUpdatedDto()
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = "Evening Exercise", Frequency = RoutineFrequency.Daily };
        var updatedRoutine = new Routine
        {
            Id = 1,
            Name = "Evening Exercise",
            Frequency = RoutineFrequency.Daily,
            UserId = TestUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Routine>(), TestUserId))
            .ReturnsAsync(updatedRoutine);

        // Act
        var result = await _service.UpdateRoutineAsync(1, dto, TestUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Evening Exercise");
        result.Frequency.Should().Be(RoutineFrequency.Daily);
        result.UpdatedAt.Should().NotBeNull();

        _repositoryMock.Verify(r => r.UpdateAsync(
            It.Is<Routine>(routine => routine.Id == 1 && routine.UserId == TestUserId),
            TestUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateRoutineAsync_NonExistingRoutine_ReturnsNull()
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = "Updated", Frequency = RoutineFrequency.Monthly };
        _repositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Routine>(), TestUserId))
            .ReturnsAsync((Routine?)null);

        // Act
        var result = await _service.UpdateRoutineAsync(999, dto, TestUserId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task UpdateRoutineAsync_EmptyOrWhitespaceName_ThrowsArgumentException(string invalidName)
    {
        // Arrange
        var dto = new UpdateRoutineDto { Name = invalidName, Frequency = RoutineFrequency.Daily };

        // Act
        Func<Task> act = () => _service.UpdateRoutineAsync(1, dto, TestUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Name cannot be empty or whitespace");

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Routine>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteRoutineAsync_ExistingRoutine_ReturnsTrue()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(true);

        // Act
        var result = await _service.SoftDeleteRoutineAsync(1, TestUserId);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(1, TestUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteRoutineAsync_NonExistingRoutine_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(999, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _service.SoftDeleteRoutineAsync(999, TestUserId);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.SoftDeleteAsync(999, TestUserId), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteRoutineAsync_AlreadyDeletedRoutine_ReturnsFalse()
    {
        // Arrange
        _repositoryMock.Setup(r => r.SoftDeleteAsync(1, TestUserId)).ReturnsAsync(false);

        // Act
        var result = await _service.SoftDeleteRoutineAsync(1, TestUserId);

        // Assert
        result.Should().BeFalse();
    }
}