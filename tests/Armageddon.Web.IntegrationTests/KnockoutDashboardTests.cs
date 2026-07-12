using Armageddon.Abstractions.Models;
using Armageddon.Web.Components.Pages;
using Bunit;
using FluentAssertions;
using NSubstitute;

namespace Armageddon.Web.IntegrationTests;

/// <summary>
/// Tests for KnockoutDashboard.razor — tournament bracket page rendering.
/// </summary>
public class KnockoutDashboardTests : IDisposable
{
    private readonly WebTestContext _ctx;
    public KnockoutDashboardTests() { _ctx = new WebTestContext(); _ctx.SetAuthenticated("admin", "Admin"); }
    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void KnockoutDashboard_RendersPageTitle()
    {
        _ctx.ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament { Status = TournamentStatus.NotStarted }));
        _ctx.ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>([]));
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));

        var cut = _ctx.Render<KnockoutDashboard>();
        cut.Find("h2").TextContent.Should().Contain("Tournament Bracket");
    }

    [Fact]
    public void KnockoutDashboard_ShowsNoMatchesMessage_WhenMatchesEmpty()
    {
        _ctx.ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament { Status = TournamentStatus.NotStarted }));
        _ctx.ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>([]));
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));

        var cut = _ctx.Render<KnockoutDashboard>();
        cut.Markup.Should().Contain("No draw yet");
    }

    [Fact]
    public void KnockoutDashboard_ShowsRandomiseButton_WhenNotStarted()
    {
        _ctx.ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament { Status = TournamentStatus.NotStarted }));
        _ctx.ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>([]));
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));

        var cut = _ctx.Render<KnockoutDashboard>();
        cut.Markup.Should().Contain("Randomise Draw");
    }

    [Fact]
    public void KnockoutDashboard_ShowsStartButton_WhenMatchesExistAndNotStarted()
    {
        var teams   = new List<Team>  { new() { Id = 1, Name = "Alpha" }, new() { Id = 2, Name = "Beta" } };
        var matches = new List<Match> { new() { Id = 1, RoundNumber = 1, TeamAId = 1, TeamBId = 2, Status = MatchStatus.Pending } };

        _ctx.ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament
        {
            Status  = TournamentStatus.NotStarted,
            Matches = matches
        }));
        _ctx.ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>(matches));
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>(teams));

        var cut = _ctx.Render<KnockoutDashboard>();
        cut.Markup.Should().Contain("Start Tournament");
    }

    [Fact]
    public void KnockoutDashboard_ShowsResetButton_WhenTournamentInProgress()
    {
        _ctx.ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament { Status = TournamentStatus.InProgress }));
        _ctx.ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>([]));
        _ctx.ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));

        var cut = _ctx.Render<KnockoutDashboard>();
        cut.Markup.Should().Contain("Reset Tournament");
    }
}
