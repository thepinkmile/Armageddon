using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Armageddon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Armageddon.Tests.Api;

public class ScoreServiceTests
{
    private static ArmageddonDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ArmageddonDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ArmageddonDbContext(options);
    }

    private static async Task<(Team team, Round round, Objective objective)> SeedDependenciesAsync(
        ArmageddonDbContext context,
        ObjectiveType type = ObjectiveType.Recurring,
        int? maxUsage = null)
    {
        var team = new Team { Name = "Alpha" };
        var round = new Round { Number = 1 };
        var objective = new Objective { Name = "Kills", Type = type, MaxUsage = maxUsage };
        context.Teams.Add(team);
        context.Rounds.Add(round);
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();
        return (team, round, objective);
    }

    // ── Rounds ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoundsAsync_ReturnsRoundsOrderedByNumber()
    {
        await using var context = CreateContext(nameof(GetRoundsAsync_ReturnsRoundsOrderedByNumber));
        context.Rounds.AddRange(
            new Round { Number = 3 },
            new Round { Number = 1 },
            new Round { Number = 2 });
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var result = (await service.GetRoundsAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Number);
        Assert.Equal(2, result[1].Number);
        Assert.Equal(3, result[2].Number);
    }

    [Fact]
    public async Task AddRoundAsync_AddsAndReturnsRound()
    {
        await using var context = CreateContext(nameof(AddRoundAsync_AddsAndReturnsRound));
        var service = new ScoreService(context);

        var result = await service.AddRoundAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Number);
        Assert.Equal(1, await context.Rounds.CountAsync());
    }

    // ── Basic score CRUD ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddScoreAsync_AddsAndReturnsScore()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_AddsAndReturnsScore));
        var (team, round, objective) = await SeedDependenciesAsync(context);

        var service = new ScoreService(context);
        var result = await service.AddScoreAsync(team.Id, round.Id, objective.Id, 10);

        Assert.NotNull(result);
        Assert.Equal(team.Id, result.TeamId);
        Assert.Equal(round.Id, result.RoundId);
        Assert.Equal(objective.Id, result.ObjectiveId);
        Assert.Equal(10, result.Points);
    }

    [Fact]
    public async Task GetScoresAsync_ReturnsAllScores()
    {
        await using var context = CreateContext(nameof(GetScoresAsync_ReturnsAllScores));
        var (team, round, objective) = await SeedDependenciesAsync(context);
        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 5 });
        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 8 });
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var result = (await service.GetScoresAsync()).ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetScoresByRoundAsync_ReturnsScoresForRound()
    {
        await using var context = CreateContext(nameof(GetScoresByRoundAsync_ReturnsScoresForRound));
        var (team, round, objective) = await SeedDependenciesAsync(context);
        var round2 = new Round { Number = 2 };
        context.Rounds.Add(round2);
        await context.SaveChangesAsync();

        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 5 });
        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round2.Id, ObjectiveId = objective.Id, Points = 8 });
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var result = (await service.GetScoresByRoundAsync(round.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(5, result[0].Points);
    }

    [Fact]
    public async Task GetScoresByTeamAsync_ReturnsScoresForTeam()
    {
        await using var context = CreateContext(nameof(GetScoresByTeamAsync_ReturnsScoresForTeam));
        var (team, round, objective) = await SeedDependenciesAsync(context);
        var team2 = new Team { Name = "Bravo" };
        context.Teams.Add(team2);
        await context.SaveChangesAsync();

        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 5 });
        context.Scores.Add(new Score { TeamId = team2.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 8 });
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var result = (await service.GetScoresByTeamAsync(team.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(5, result[0].Points);
    }

    [Fact]
    public async Task RemoveScoreAsync_ReturnsFalse_WhenNotExists()
    {
        await using var context = CreateContext(nameof(RemoveScoreAsync_ReturnsFalse_WhenNotExists));
        var service = new ScoreService(context);
        var result = await service.RemoveScoreAsync(99);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveScoreAsync_ReturnsTrue_AndRemovesScore()
    {
        await using var context = CreateContext(nameof(RemoveScoreAsync_ReturnsTrue_AndRemovesScore));
        var (team, round, objective) = await SeedDependenciesAsync(context);
        var score = new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 10 };
        context.Scores.Add(score);
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var result = await service.RemoveScoreAsync(score.Id);

        Assert.True(result);
        Assert.Equal(0, await context.Scores.CountAsync());
    }

    // ── AddScoreAsync – objective not found ───────────────────────────────────

    [Fact]
    public async Task AddScoreAsync_ThrowsInvalidOperation_WhenObjectiveNotFound()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_ThrowsInvalidOperation_WhenObjectiveNotFound));
        var (team, round, _) = await SeedDependenciesAsync(context);

        var service = new ScoreService(context);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddScoreAsync(team.Id, round.Id, 9999, 10));
    }

    // ── OneTime objective validation ──────────────────────────────────────────

    [Fact]
    public async Task AddScoreAsync_Succeeds_WhenOneTimeObjective_NotYetUsed()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Succeeds_WhenOneTimeObjective_NotYetUsed));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.OneTime);

        var service = new ScoreService(context);
        var result = await service.AddScoreAsync(team.Id, round.Id, objective.Id, 10);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task AddScoreAsync_Throws_WhenOneTimeObjective_AlreadyUsedByTeam()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Throws_WhenOneTimeObjective_AlreadyUsedByTeam));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.OneTime);
        context.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 10 });
        await context.SaveChangesAsync();

        var service = new ScoreService(context);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddScoreAsync(team.Id, round.Id, objective.Id, 10));

        Assert.Contains("usage limit", ex.Message);
    }

    [Fact]
    public async Task AddScoreAsync_Succeeds_WhenOneTimeObjective_DifferentTeamAlreadyUsedIt()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Succeeds_WhenOneTimeObjective_DifferentTeamAlreadyUsedIt));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.OneTime);
        var team2 = new Team { Name = "Bravo" };
        context.Teams.Add(team2);
        await context.SaveChangesAsync();

        // team2 already used the one-time objective
        context.Scores.Add(new Score { TeamId = team2.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 10 });
        await context.SaveChangesAsync();

        // team1 should still be allowed
        var service = new ScoreService(context);
        var result = await service.AddScoreAsync(team.Id, round.Id, objective.Id, 5);

        Assert.NotNull(result);
    }

    // ── MaxUsage validation (Recurring) ──────────────────────────────────────

    [Fact]
    public async Task AddScoreAsync_Succeeds_WhenRecurring_UnlimitedMaxUsage()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Succeeds_WhenRecurring_UnlimitedMaxUsage));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.Recurring, null);

        var service = new ScoreService(context);
        for (var i = 0; i < 5; i++)
            await service.AddScoreAsync(team.Id, round.Id, objective.Id, 1);

        Assert.Equal(5, await context.Scores.CountAsync());
    }

    [Fact]
    public async Task AddScoreAsync_Succeeds_WhenRecurring_NegativeOneMaxUsage_MeansUnlimited()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Succeeds_WhenRecurring_NegativeOneMaxUsage_MeansUnlimited));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.Recurring, -1);

        var service = new ScoreService(context);
        for (var i = 0; i < 3; i++)
            await service.AddScoreAsync(team.Id, round.Id, objective.Id, 1);

        Assert.Equal(3, await context.Scores.CountAsync());
    }

    [Fact]
    public async Task AddScoreAsync_Throws_WhenRecurring_MaxUsageExceeded()
    {
        await using var context = CreateContext(nameof(AddScoreAsync_Throws_WhenRecurring_MaxUsageExceeded));
        var (team, round, objective) = await SeedDependenciesAsync(context, ObjectiveType.Recurring, 2);

        var service = new ScoreService(context);
        await service.AddScoreAsync(team.Id, round.Id, objective.Id, 1);
        await service.AddScoreAsync(team.Id, round.Id, objective.Id, 1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddScoreAsync(team.Id, round.Id, objective.Id, 1));

        Assert.Contains("usage limit", ex.Message);
    }

    // ── EffectiveMaxUsage unit tests ──────────────────────────────────────────

    [Theory]
    [InlineData(ObjectiveType.OneTime, null, 1)]
    [InlineData(ObjectiveType.OneTime, -1, 1)]
    [InlineData(ObjectiveType.OneTime, 3, 1)]   // OneTime wins with min(1,3)=1
    [InlineData(ObjectiveType.Recurring, null, -1)]
    [InlineData(ObjectiveType.Recurring, -1, -1)]
    [InlineData(ObjectiveType.Recurring, 5, 5)]
    public void EffectiveMaxUsage_ReturnsExpected(ObjectiveType type, int? maxUsage, int expected)
    {
        var objective = new Objective { Type = type, MaxUsage = maxUsage };
        Assert.Equal(expected, ScoreService.EffectiveMaxUsage(objective));
    }
}
