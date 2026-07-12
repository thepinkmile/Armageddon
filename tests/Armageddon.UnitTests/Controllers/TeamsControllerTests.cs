using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.UnitTests.Controllers;

public class TeamsControllerTests
{
    private readonly ITeamService _service = Substitute.For<ITeamService>();
    private readonly TeamsController _sut;

    public TeamsControllerTests() => _sut = new TeamsController(_service);

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithTeams()
    {
        var teams = new List<Team> { new() { Id = 1, Name = "Alpha" } };
        _service.GetAllTeamsAsync().Returns(teams);

        var result = await _sut.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(teams);
    }

    // ── GetById ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _service.GetTeamByIdAsync(99).Returns((Team?)null);

        var result = await _sut.GetById(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Returns200WithTeam_WhenFound()
    {
        var team = new Team { Id = 5, Name = "Bravo" };
        _service.GetTeamByIdAsync(5).Returns(team);

        var result = await _sut.GetById(5);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(team);
    }

    // ── Add ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_Returns400_WhenNameEmpty()
    {
        var result = await _sut.Add("   ");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Add_Returns201WithTeam_WhenValid()
    {
        var team = new Team { Id = 7, Name = "Charlie" };
        _service.AddTeamAsync("Charlie").Returns(team);

        var result = await _sut.Add("Charlie");

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(team);
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_Returns404_WhenNotFound()
    {
        _service.RemoveTeamAsync(99).Returns(false);

        var result = await _sut.Remove(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Remove_Returns204_WhenRemoved()
    {
        _service.RemoveTeamAsync(1).Returns(true);

        var result = await _sut.Remove(1);

        result.Should().BeOfType<NoContentResult>();
    }
}
