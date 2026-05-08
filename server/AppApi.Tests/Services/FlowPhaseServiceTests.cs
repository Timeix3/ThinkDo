// AppApi.Tests/Services/FlowPhaseServiceTests.cs

using AppApi.Repositories.Interfaces;
using AppApi.Services;
using Common.Models;
using FluentAssertions;
using Moq;

namespace AppApi.Tests.Services;

public class FlowPhaseServiceTests
{
    private readonly Mock<IFlowPhaseRepository> _repositoryMock;
    private readonly FlowPhaseService _service;
    private const string TestUserId = "test-user-123";

    public FlowPhaseServiceTests()
    {
        _repositoryMock = new Mock<IFlowPhaseRepository>();
        _service = new FlowPhaseService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetPhaseAsync_ExistingRecord_ReturnsPhase()
    {
        // Arrange
        var record = new UserFlowPhase
        {
            UserId = TestUserId,
            Phase = "sprint",
            UpdatedAt = DateTime.UtcNow
        };

        _repositoryMock
            .Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync(record);

        // Act
        var result = await _service.GetPhaseAsync(TestUserId);

        // Assert
        result.Should().Be("sprint");
    }

    [Fact]
    public async Task GetPhaseAsync_NewUser_ReturnsDefaultPlanning()
    {
        // Arrange (краевой случай: пользователь никогда не менял фазу)
        _repositoryMock
            .Setup(r => r.GetByUserIdAsync(TestUserId))
            .ReturnsAsync((UserFlowPhase?)null);

        // Act
        var result = await _service.GetPhaseAsync(TestUserId);

        // Assert
        result.Should().Be("planning");
    }

    [Theory]
    [InlineData("sprint")]
    [InlineData("review")]
    [InlineData("planning")]
    [InlineData("SPRINT")]
    [InlineData("Review")]
    [InlineData("PLANNING")]
    public async Task SetPhaseAsync_ValidPhase_NormalizesAndSaves(string inputPhase)
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.UpsertAsync(TestUserId, inputPhase.ToLowerInvariant()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SetPhaseAsync(TestUserId, inputPhase);

        // Assert
        result.Should().Be(inputPhase.ToLowerInvariant());
        _repositoryMock.Verify(
            r => r.UpsertAsync(TestUserId, inputPhase.ToLowerInvariant()),
            Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("unknown")]
    [InlineData("sprint ")]
    public async Task SetPhaseAsync_InvalidPhase_ThrowsArgumentException(string? invalidPhase)
    {
        // Act & Assert
        await _service
            .Invoking(s => s.SetPhaseAsync(TestUserId, invalidPhase!))
            .Should()
            .ThrowAsync<ArgumentException>();

        _repositoryMock.Verify(
            r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}