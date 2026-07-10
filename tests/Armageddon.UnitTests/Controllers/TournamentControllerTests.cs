using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.UnitTests.Controllers;

public class TournamentControllerTests
{
    private readonly ITournamentService _service = Substitute.For<ITournamentService>();
    private readonly TournamentController _sut;

    public TournamentControllerTests() => _sut = new TournamentController(_service);

    // ── Get ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Returns200WithTournament()
    {
        var tournament = new Tournament { Status = TournamentStatus.NotStarted };
        _service.GetOrCreateAsync().Returns(tournament);

        var result = await _sut.Get();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(tournament);
    }

    // ── Randomise ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Randomise_Returns200_WhenSuccessful()
    {
        var tournament = new Tournament();
        _service.RandomiseAsync().Returns(tournament);

        var result = await _sut.Randomise();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Randomise_Returns400_WhenInvalidOperation()
    {
        _service.RandomiseAsync().ThrowsAsync(new InvalidOperationException("already started"));

        var result = await _sut.Randomise();

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().Be("already started");
    }

    // ── Start ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Start_Returns200_WhenSuccessful()
    {
        _service.StartAsync().Returns(new Tournament { Status = TournamentStatus.InProgress });
        var result = await _sut.Start();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Start_Returns400_WhenInvalidOperation()
    {
        _service.StartAsync().ThrowsAsync(new InvalidOperationException("no matches"));
        var result = await _sut.Start();
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetMatches ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetMatches_Returns200()
    {
        _service.GetMatchesAsync().Returns(new List<Match>());
        var result = await _sut.GetMatches();
        result.Should().BeOfType<OkObjectResult>();
    }

    // ── StartMatch ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StartMatch_Returns200_WhenSuccessful()
    {
        var match = new Match { Id = 1, Status = MatchStatus.InProgress };
        _service.StartMatchAsync(1).Returns(match);

        var result = await _sut.StartMatch(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task StartMatch_Returns400_WhenInvalidOperation()
    {
        _service.StartMatchAsync(99).ThrowsAsync(new InvalidOperationException("not found"));
        var result = await _sut.StartMatch(99);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── CompleteMatch ──────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteMatch_Returns200_WhenSuccessful()
    {
        _service.CompleteMatchAsync(1).Returns(new Match { Id = 1, Status = MatchStatus.Completed });
        var result = await _sut.CompleteMatch(1);
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CompleteMatch_Returns400_WhenInvalidOperation()
    {
        _service.CompleteMatchAsync(99).ThrowsAsync(new InvalidOperationException("already done"));
        var result = await _sut.CompleteMatch(99);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Reset ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reset_Returns204()
    {
        _service.ResetAsync().Returns(Task.CompletedTask);
        var result = await _sut.Reset();
        result.Should().BeOfType<NoContentResult>();
    }

    // ── Finalise ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Finalise_Returns200_WhenSuccessful()
    {
        _service.FinaliseTournamentAsync().Returns(new TournamentResult { WinnerName = "Alpha" });
        var result = await _sut.Finalise();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Finalise_Returns400_WhenInvalidOperation()
    {
        _service.FinaliseTournamentAsync().ThrowsAsync(new InvalidOperationException("final not complete"));
        var result = await _sut.Finalise();
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetResults ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetResults_Returns200()
    {
        _service.GetResultsAsync().Returns(new List<TournamentResult>());
        var result = await _sut.GetResults();
        result.Should().BeOfType<OkObjectResult>();
    }
}
