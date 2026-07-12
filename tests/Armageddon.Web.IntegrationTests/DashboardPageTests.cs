using Armageddon.Abstractions.Models;
using Armageddon.Web.Components.Pages;
using Bunit;
using FluentAssertions;
using NSubstitute;

namespace Armageddon.Web.IntegrationTests;

/// <summary>
/// Tests for Dashboard.razor — IArmageddonApiClient is mocked, no real HTTP calls.
/// </summary>
public class DashboardPageTests : IDisposable
{
    private readonly WebTestContext _ctx;
    public DashboardPageTests() { _ctx = new WebTestContext(); _ctx.SetAuthenticated("admin", "Admin"); }
    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Dashboard_ShowsNoTeamsMessage_WhenApiReturnsEmptyTeams()
    {
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));
        _ctx.ApiClient.GetRoundsAsync().Returns(Task.FromResult<IEnumerable<Round>>([]));
        _ctx.ApiClient.GetScoresAsync().Returns(Task.FromResult<IEnumerable<Score>>([]));

        var cut = _ctx.Render<Dashboard>();
        cut.Markup.Should().Contain("No teams yet");
    }

    [Fact]
    public void Dashboard_ShowsScoreTable_WhenTeamsAndRoundsExist()
    {
        var teams  = new List<Team>  { new() { Id = 1, Name = "Alpha" }, new() { Id = 2, Name = "Beta" } };
        var rounds = new List<Round> { new() { Id = 1, Number = 1 } };

        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>(teams));
        _ctx.ApiClient.GetRoundsAsync().Returns(Task.FromResult<IEnumerable<Round>>(rounds));
        _ctx.ApiClient.GetScoresAsync().Returns(Task.FromResult<IEnumerable<Score>>([]));

        var cut = _ctx.Render<Dashboard>();
        cut.Find("table").Should().NotBeNull();
        cut.Markup.Should().Contain("Alpha").And.Contain("Beta");
    }

    [Fact]
    public void Dashboard_ShowsTeamTotalScore_FromScores()
    {
        var teams  = new List<Team>  { new() { Id = 1, Name = "Alpha" } };
        var rounds = new List<Round> { new() { Id = 1, Number = 1 } };
        var scores = new List<Score> { new() { Id = 1, TeamId = 1, RoundId = 1, Points = 150 } };

        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>(teams));
        _ctx.ApiClient.GetRoundsAsync().Returns(Task.FromResult<IEnumerable<Round>>(rounds));
        _ctx.ApiClient.GetScoresAsync().Returns(Task.FromResult<IEnumerable<Score>>(scores));

        var cut = _ctx.Render<Dashboard>();
        cut.Markup.Should().Contain("150");
    }

    [Fact]
    public void Dashboard_RendersPageTitle()
    {
        var cut = _ctx.Render<Dashboard>();
        cut.Find("h2").TextContent.Should().Contain("Score Card");
    }
}
