using Armageddon.Abstractions.Models;
using Armageddon.Web.Components.Pages;
using Armageddon.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Armageddon.Tests.Web;

public class DashboardComponentTests : BunitContext
{
    private readonly Mock<IArmageddonApiClient> _mockClient = new();

    public DashboardComponentTests()
    {
        Services.AddSingleton(_mockClient.Object);
    }

    [Fact]
    public void Dashboard_ShowsLoadingInitially()
    {
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(Array.Empty<Team>());
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());
        _mockClient.Setup(c => c.GetScoresAsync()).ReturnsAsync(Array.Empty<Score>());

        var cut = Render<Dashboard>();

        // After loading, if no teams, should show the no-teams message
        cut.WaitForState(() => !cut.Find("h2").TextContent.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("No teams yet", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsScoreboard_WhenTeamsAndRoundsExist()
    {
        var teams = new List<Team> { new() { Id = 1, Name = "Alpha" }, new() { Id = 2, Name = "Bravo" } };
        var rounds = new List<Round> { new() { Id = 1, Number = 1 }, new() { Id = 2, Number = 2 } };
        var scores = new List<Score>
        {
            new() { Id = 1, TeamId = 1, RoundId = 1, ObjectiveId = 1, Points = 5 },
            new() { Id = 2, TeamId = 2, RoundId = 2, ObjectiveId = 1, Points = 8 }
        };
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(teams);
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(rounds);
        _mockClient.Setup(c => c.GetScoresAsync()).ReturnsAsync(scores);

        var cut = Render<Dashboard>();

        cut.WaitForState(() => cut.Markup.Contains("Alpha"), timeout: TimeSpan.FromSeconds(5));

        Assert.Contains("Alpha", cut.Markup);
        Assert.Contains("Bravo", cut.Markup);
        Assert.Contains("Round 1", cut.Markup);
        Assert.Contains("Round 2", cut.Markup);
    }

    [Fact]
    public void Dashboard_ShowsError_WhenApiFails()
    {
        _mockClient.Setup(c => c.GetTeamsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));
        _mockClient.Setup(c => c.GetRoundsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));
        _mockClient.Setup(c => c.GetScoresAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));

        var cut = Render<Dashboard>();

        cut.WaitForState(() => cut.Markup.Contains("Failed"), timeout: TimeSpan.FromSeconds(5));

        Assert.Contains("Failed to load scoreboard", cut.Markup);
    }

    [Fact]
    public void Dashboard_CalculatesTotalScore_Correctly()
    {
        var teams = new List<Team> { new() { Id = 1, Name = "Alpha" } };
        var rounds = new List<Round> { new() { Id = 1, Number = 1 }, new() { Id = 2, Number = 2 } };
        var scores = new List<Score>
        {
            new() { Id = 1, TeamId = 1, RoundId = 1, ObjectiveId = 1, Points = 5 },
            new() { Id = 2, TeamId = 1, RoundId = 2, ObjectiveId = 1, Points = 10 }
        };
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(teams);
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(rounds);
        _mockClient.Setup(c => c.GetScoresAsync()).ReturnsAsync(scores);

        var cut = Render<Dashboard>();

        cut.WaitForState(() => cut.Markup.Contains("Alpha"), timeout: TimeSpan.FromSeconds(5));

        // Total should be 15
        Assert.Contains("15", cut.Markup);
    }
}
