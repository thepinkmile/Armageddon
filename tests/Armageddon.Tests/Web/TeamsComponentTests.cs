using Armageddon.Abstractions.Models;
using Armageddon.Web.Components.Pages;
using Armageddon.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Armageddon.Tests.Web;

public class TeamsComponentTests : BunitContext
{
    private readonly Mock<IArmageddonApiClient> _mockClient = new();

    public TeamsComponentTests()
    {
        Services.AddSingleton(_mockClient.Object);
    }

    [Fact]
    public void Teams_ShowsNoTeamsMessage_WhenEmpty()
    {
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(Array.Empty<Team>());

        var cut = Render<Teams>();

        cut.WaitForState(() => cut.Markup.Contains("No teams yet"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("No teams yet", cut.Markup);
    }

    [Fact]
    public void Teams_ShowsTeamList_WhenTeamsExist()
    {
        var teams = new List<Team>
        {
            new() { Id = 1, Name = "Alpha" },
            new() { Id = 2, Name = "Bravo" }
        };
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(teams);

        var cut = Render<Teams>();

        cut.WaitForState(() => cut.Markup.Contains("Alpha"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("Alpha", cut.Markup);
        Assert.Contains("Bravo", cut.Markup);
    }

    [Fact]
    public async Task Teams_AddTeam_CallsApiAndRefreshes()
    {
        var teams = new List<Team>();
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(() => teams.ToList());
        _mockClient.Setup(c => c.AddTeamAsync("Charlie"))
            .Callback<string>(name => teams.Add(new Team { Id = 1, Name = name }))
            .ReturnsAsync(new Team { Id = 1, Name = "Charlie" });

        var cut = Render<Teams>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        var input = cut.Find("input.form-control");
        await cut.InvokeAsync(() => input.Input("Charlie"));

        // Trigger the add button
        var button = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => button.Click());

        _mockClient.Verify(c => c.AddTeamAsync("Charlie"), Times.Once);
    }

    [Fact]
    public async Task Teams_RemoveTeam_CallsApiAndRefreshes()
    {
        var teams = new List<Team> { new() { Id = 1, Name = "Alpha" } };
        _mockClient.Setup(c => c.GetTeamsAsync()).ReturnsAsync(() => teams.ToList());
        _mockClient.Setup(c => c.RemoveTeamAsync(1))
            .Callback<int>(_ => teams.Clear())
            .Returns(Task.CompletedTask);

        var cut = Render<Teams>();

        cut.WaitForState(() => cut.Markup.Contains("Alpha"), timeout: TimeSpan.FromSeconds(5));

        var removeButton = cut.Find("button.btn-danger");
        await cut.InvokeAsync(() => removeButton.Click());

        _mockClient.Verify(c => c.RemoveTeamAsync(1), Times.Once);
    }

    [Fact]
    public void Teams_ShowsError_WhenApiFails()
    {
        _mockClient.Setup(c => c.GetTeamsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));

        var cut = Render<Teams>();

        cut.WaitForState(() => cut.Markup.Contains("Failed"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("Failed to load teams", cut.Markup);
    }
}
