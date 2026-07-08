using Armageddon.Abstractions.Models;
using Armageddon.Web.Components.Pages;
using Armageddon.Web.Services;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Armageddon.Tests.Web;

public class ScoringComponentTests : BunitContext
{
    private readonly Mock<IArmageddonApiClient> _mockClient = new();

    public ScoringComponentTests()
    {
        Services.AddSingleton(_mockClient.Object);
    }

    [Fact]
    public void Scoring_ShowsNoObjectivesMessage_WhenEmpty()
    {
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(Array.Empty<Objective>());
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("No objectives yet"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("No objectives yet", cut.Markup);
    }

    [Fact]
    public void Scoring_ShowsObjectiveList_WhenObjectivesExist()
    {
        var objectives = new List<Objective>
        {
            new() { Id = 1, Name = "Kills" },
            new() { Id = 2, Name = "Captures" }
        };
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(objectives);
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("Kills"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("Kills", cut.Markup);
        Assert.Contains("Captures", cut.Markup);
    }

    [Fact]
    public void Scoring_ShowsOneTimeBadge_ForOneTimeObjective()
    {
        var objectives = new List<Objective>
        {
            new() { Id = 1, Name = "First Blood", Type = ObjectiveType.OneTime }
        };
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(objectives);
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("First Blood"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("One-time", cut.Markup);
    }

    [Fact]
    public void Scoring_ShowsMaxUsageBadge_WhenMaxUsageSet()
    {
        var objectives = new List<Objective>
        {
            new() { Id = 1, Name = "Capture Flag", Type = ObjectiveType.Recurring, MaxUsage = 3 }
        };
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(objectives);
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("Capture Flag"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("Max: 3", cut.Markup);
    }

    [Fact]
    public async Task Scoring_AddObjective_CallsApiWithTypeAndMaxUsage()
    {
        var objectives = new List<Objective>();
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(() => objectives.ToList());
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());
        _mockClient.Setup(c => c.AddObjectiveAsync("Assists", ObjectiveType.Recurring, null))
            .Callback<string, ObjectiveType, int?>((name, _, _) =>
                objectives.Add(new Objective { Id = 1, Name = name }))
            .ReturnsAsync(new Objective { Id = 1, Name = "Assists" });

        var cut = Render<Scoring>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        var input = cut.Find("input.form-control");
        await cut.InvokeAsync(() => input.Input("Assists"));

        var addButton = cut.Find("button.btn-primary");
        await cut.InvokeAsync(() => addButton.Click());

        _mockClient.Verify(c => c.AddObjectiveAsync("Assists", ObjectiveType.Recurring, null), Times.Once);
    }

    [Fact]
    public async Task Scoring_RemoveObjective_CallsApi()
    {
        var objectives = new List<Objective> { new() { Id = 1, Name = "Kills" } };
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(() => objectives.ToList());
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(Array.Empty<Round>());
        _mockClient.Setup(c => c.RemoveObjectiveAsync(1))
            .Callback<int>(_ => objectives.Clear())
            .Returns(Task.CompletedTask);

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("Kills"), timeout: TimeSpan.FromSeconds(5));

        var removeButton = cut.Find("button.btn-danger");
        await cut.InvokeAsync(() => removeButton.Click());

        _mockClient.Verify(c => c.RemoveObjectiveAsync(1), Times.Once);
    }

    [Fact]
    public async Task Scoring_AddRound_CallsApiWithNextNumber()
    {
        var rounds = new List<Round>();
        _mockClient.Setup(c => c.GetObjectivesAsync()).ReturnsAsync(Array.Empty<Objective>());
        _mockClient.Setup(c => c.GetRoundsAsync()).ReturnsAsync(() => rounds.ToList());
        _mockClient.Setup(c => c.AddRoundAsync(1))
            .Callback<int>(n => rounds.Add(new Round { Id = 1, Number = n }))
            .ReturnsAsync(new Round { Id = 1, Number = 1 });

        var cut = Render<Scoring>();

        cut.WaitForState(() => !cut.Markup.Contains("Loading"), timeout: TimeSpan.FromSeconds(5));

        // Find the "Add Round 1" button by its text content
        var addRoundButton = cut.FindAll("button.btn-primary")
            .First(b => b.TextContent.Contains("Add Round"));
        await cut.InvokeAsync(() => addRoundButton.Click());

        _mockClient.Verify(c => c.AddRoundAsync(1), Times.Once);
    }

    [Fact]
    public void Scoring_ShowsError_WhenApiFails()
    {
        _mockClient.Setup(c => c.GetObjectivesAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));
        _mockClient.Setup(c => c.GetRoundsAsync()).ThrowsAsync(new HttpRequestException("Connection refused"));

        var cut = Render<Scoring>();

        cut.WaitForState(() => cut.Markup.Contains("Failed"), timeout: TimeSpan.FromSeconds(5));
        Assert.Contains("Failed to load data", cut.Markup);
    }
}
