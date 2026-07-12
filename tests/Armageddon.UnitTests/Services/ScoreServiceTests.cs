using Armageddon.Abstractions.Models;
using Armageddon.Api.Services;
using Armageddon.UnitTests.Helpers;
using FluentAssertions;

namespace Armageddon.UnitTests.Services;

public class ScoreServiceTests
{
    // ── Helpers ────────────────────────────────────────────────────────────

    private static async Task<(Team team, Round round, Objective objective)> SeedBasicAsync(
        Armageddon.Api.Data.ArmageddonDbContext ctx,
        int? maxUsage = null)
    {
        var team = new Team { Name = "Team A" };
        var round = new Round { Number = 1 };
        var objective = new Objective { Name = "Kill", Points = 100, MaxUsage = maxUsage };
        ctx.Teams.Add(team);
        ctx.Rounds.Add(round);
        ctx.Objectives.Add(objective);
        await ctx.SaveChangesAsync();
        return (team, round, objective);
    }

    // ── GetRoundsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoundsAsync_ReturnsEmpty_WhenNone()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ScoreService(ctx);

        var result = await sut.GetRoundsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRoundsAsync_ReturnsRoundsOrderedByNumber()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Rounds.AddRange(new Round { Number = 3 }, new Round { Number = 1 }, new Round { Number = 2 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var result = (await sut.GetRoundsAsync()).ToList();

        result.Select(r => r.Number).Should().ContainInOrder(1, 2, 3);
    }

    // ── AddRoundAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddRoundAsync_PersistsRound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ScoreService(ctx);

        var result = await sut.AddRoundAsync(42);

        result.Id.Should().BeGreaterThan(0);
        result.Number.Should().Be(42);
        ctx.Rounds.Should().ContainSingle(r => r.Number == 42);
    }

    // ── GetScoresAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetScoresAsync_ReturnsEmpty_WhenNone()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ScoreService(ctx);

        var result = await sut.GetScoresAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetScoresAsync_ReturnsAllScoresWithIncludes()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx);
        ctx.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var result = (await sut.GetScoresAsync()).ToList();

        result.Should().HaveCount(1);
        result[0].Team.Should().NotBeNull();
        result[0].Round.Should().NotBeNull();
        result[0].Objective.Should().NotBeNull();
    }

    // ── GetScoresByRoundAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetScoresByRoundAsync_ReturnsOnlyMatchingRound()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx);
        var round2 = new Round { Number = 2 };
        ctx.Rounds.Add(round2);
        await ctx.SaveChangesAsync();
        ctx.Scores.AddRange(
            new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 },
            new Score { TeamId = team.Id, RoundId = round2.Id, ObjectiveId = objective.Id, Points = 50 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var result = await sut.GetScoresByRoundAsync(round.Id);

        result.Should().ContainSingle(s => s.RoundId == round.Id);
    }

    // ── GetScoresByTeamAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetScoresByTeamAsync_ReturnsOnlyMatchingTeam()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx);
        var team2 = new Team { Name = "Team B" };
        ctx.Teams.Add(team2);
        await ctx.SaveChangesAsync();
        ctx.Scores.AddRange(
            new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 },
            new Score { TeamId = team2.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 200 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var result = await sut.GetScoresByTeamAsync(team.Id);

        result.Should().ContainSingle(s => s.TeamId == team.Id);
    }

    // ── AddScoreAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddScoreAsync_ThrowsInvalidOperation_WhenObjectiveNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, _) = await SeedBasicAsync(ctx);
        var sut = new ScoreService(ctx);

        var act = () => sut.AddScoreAsync(team.Id, round.Id, 999, 100);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Objective 999 not found*");
    }

    [Fact]
    public async Task AddScoreAsync_PersistsScore_WhenUnlimitedObjective()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx, maxUsage: null);
        var sut = new ScoreService(ctx);

        var result = await sut.AddScoreAsync(team.Id, round.Id, objective.Id, 100);

        result.Id.Should().BeGreaterThan(0);
        result.Points.Should().Be(100);
    }

    [Fact]
    public async Task AddScoreAsync_PersistsScore_WhenWithinUsageLimit()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx, maxUsage: 2);
        var sut = new ScoreService(ctx);

        // First use — should succeed
        var result = await sut.AddScoreAsync(team.Id, round.Id, objective.Id, 100);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AddScoreAsync_ThrowsInvalidOperation_WhenUsageLimitExceeded()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx, maxUsage: 1);
        ctx.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var act = () => sut.AddScoreAsync(team.Id, round.Id, objective.Id, 100);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*usage limit*");
    }

    [Fact]
    public async Task AddScoreAsync_AllowsSameObjectiveInDifferentRounds()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx, maxUsage: 1);
        var round2 = new Round { Number = 2 };
        ctx.Rounds.Add(round2);
        await ctx.SaveChangesAsync();
        // Use limit in round 1
        ctx.Scores.Add(new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 });
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        // Should succeed in round 2 (per-match scope)
        var result = await sut.AddScoreAsync(team.Id, round2.Id, objective.Id, 100);

        result.Should().NotBeNull();
    }

    // ── RemoveScoreAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task RemoveScoreAsync_ReturnsFalse_WhenNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ScoreService(ctx);

        var result = await sut.RemoveScoreAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveScoreAsync_ReturnsTrue_AndRemovesScore()
    {
        using var ctx = DbContextFactory.Create();
        var (team, round, objective) = await SeedBasicAsync(ctx);
        var score = new Score { TeamId = team.Id, RoundId = round.Id, ObjectiveId = objective.Id, Points = 100 };
        ctx.Scores.Add(score);
        await ctx.SaveChangesAsync();
        var sut = new ScoreService(ctx);

        var result = await sut.RemoveScoreAsync(score.Id);

        result.Should().BeTrue();
        ctx.Scores.Should().BeEmpty();
    }

    // ── EffectiveMaxUsage ──────────────────────────────────────────────────

    [Theory]
    [InlineData(null, -1)]
    [InlineData(0, -1)]
    [InlineData(-1, -1)]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    public void EffectiveMaxUsage_ReturnsExpectedValue(int? maxUsage, int expected)
    {
        var objective = new Objective { Name = "X", Points = 10, MaxUsage = maxUsage };

        var result = ScoreService.EffectiveMaxUsage(objective);

        result.Should().Be(expected);
    }
}
