using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.UnitTests.Controllers;

public class ScoresControllerTests
{
    private readonly IScoreService _service = Substitute.For<IScoreService>();
    private readonly ScoresController _sut;

    public ScoresControllerTests() => _sut = new ScoresController(_service);

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200()
    {
        _service.GetScoresAsync().Returns(new List<Score>());
        var result = await _sut.GetAll();
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetByRound ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByRound_Returns200()
    {
        _service.GetScoresByRoundAsync(1).Returns(new List<Score>());
        var result = await _sut.GetByRound(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetByTeam ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByTeam_Returns200()
    {
        _service.GetScoresByTeamAsync(1).Returns(new List<Score>());
        var result = await _sut.GetByTeam(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── GetRounds ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRounds_Returns200()
    {
        _service.GetRoundsAsync().Returns(new List<Round>());
        var result = await _sut.GetRounds();
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── AddRound ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddRound_Returns201WithRound()
    {
        var round = new Round { Id = 1, Number = 42 };
        _service.AddRoundAsync(42).Returns(round);

        var result = await _sut.AddRound(42);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(round);
    }

    // ── AddScore ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddScore_Returns201_WhenSuccessful()
    {
        var score = new Score { Id = 1, TeamId = 1, RoundId = 1, ObjectiveId = 1, Points = 100 };
        _service.AddScoreAsync(1, 1, 1, 100).Returns(score);

        var result = await _sut.AddScore(new AddScoreRequest(1, 1, 1, 100));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(score);
    }

    [Fact]
    public async Task AddScore_Returns400_WhenInvalidOperation()
    {
        _service.AddScoreAsync(1, 1, 1, 100)
            .ThrowsAsync(new InvalidOperationException("usage limit exceeded"));

        var result = await _sut.AddScore(new AddScoreRequest(1, 1, 1, 100));

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().Be("usage limit exceeded");
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_Returns404_WhenNotFound()
    {
        _service.RemoveScoreAsync(99).Returns(false);
        var result = await _sut.Remove(99);
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Remove_Returns204_WhenRemoved()
    {
        _service.RemoveScoreAsync(1).Returns(true);
        var result = await _sut.Remove(1);
        result.Should().BeOfType<NoContentResult>();
    }
}
