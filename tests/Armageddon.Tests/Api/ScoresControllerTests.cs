using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Tests.Api;

public class ScoresControllerTests
{
    private readonly Mock<IScoreService> _mockService = new();
    private ScoresController CreateController() => new(_mockService.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithScores()
    {
        var scores = new List<Score> { new() { Id = 1, Points = 10 } };
        _mockService.Setup(s => s.GetScoresAsync()).ReturnsAsync(scores);

        var result = await CreateController().GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scores, ok.Value);
    }

    [Fact]
    public async Task GetByRound_ReturnsOkWithScores()
    {
        var scores = new List<Score> { new() { Id = 1, RoundId = 2, Points = 5 } };
        _mockService.Setup(s => s.GetScoresByRoundAsync(2)).ReturnsAsync(scores);

        var result = await CreateController().GetByRound(2);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scores, ok.Value);
    }

    [Fact]
    public async Task GetByTeam_ReturnsOkWithScores()
    {
        var scores = new List<Score> { new() { Id = 1, TeamId = 3, Points = 7 } };
        _mockService.Setup(s => s.GetScoresByTeamAsync(3)).ReturnsAsync(scores);

        var result = await CreateController().GetByTeam(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(scores, ok.Value);
    }

    [Fact]
    public async Task GetRounds_ReturnsOkWithRounds()
    {
        var rounds = new List<Round> { new() { Id = 1, Number = 1 } };
        _mockService.Setup(s => s.GetRoundsAsync()).ReturnsAsync(rounds);

        var result = await CreateController().GetRounds();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(rounds, ok.Value);
    }

    [Fact]
    public async Task AddRound_ReturnsCreated()
    {
        var round = new Round { Id = 1, Number = 1 };
        _mockService.Setup(s => s.AddRoundAsync(1)).ReturnsAsync(round);

        var result = await CreateController().AddRound(1);

        Assert.IsType<CreatedAtActionResult>(result);
    }

    [Fact]
    public async Task AddScore_ReturnsCreated_WithScore()
    {
        var score = new Score { Id = 1, TeamId = 1, RoundId = 1, ObjectiveId = 1, Points = 10 };
        _mockService.Setup(s => s.AddScoreAsync(1, 1, 1, 10)).ReturnsAsync(score);

        var result = await CreateController().AddScore(new AddScoreRequest(1, 1, 1, 10));

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(score, created.Value);
    }

    [Fact]
    public async Task Remove_ReturnsNoContent_WhenSuccessful()
    {
        _mockService.Setup(s => s.RemoveScoreAsync(1)).ReturnsAsync(true);

        var result = await CreateController().Remove(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.RemoveScoreAsync(99)).ReturnsAsync(false);

        var result = await CreateController().Remove(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
