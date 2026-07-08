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

    private static async Task<(Team team, Round round, Objective objective)> SeedDependenciesAsync(ArmageddonDbContext context)
    {
        var team = new Team { Name = "Alpha" };
        var round = new Round { Number = 1 };
        var objective = new Objective { Name = "Kills" };
        context.Teams.Add(team);
        context.Rounds.Add(round);
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();
        return (team, round, objective);
    }

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
}
