using Armageddon.Abstractions.Models;
using Armageddon.Api.Services;
using Armageddon.UnitTests.Helpers;
using FluentAssertions;

namespace Armageddon.UnitTests.Services;

public class TournamentServiceTests
{
    // ── Seed helpers ───────────────────────────────────────────────────────

    private static async Task<List<Team>> SeedTeamsAsync(
        Armageddon.Api.Data.ArmageddonDbContext ctx, int count)
    {
        var teams = Enumerable.Range(1, count)
            .Select(i => new Team { Name = $"Team {i}" })
            .ToList();
        ctx.Teams.AddRange(teams);
        await ctx.SaveChangesAsync();
        return teams;
    }

    // ── GetOrCreateAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreateAsync_CreatesNewTournament_WhenNoneExists()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var result = await sut.GetOrCreateAsync();

        result.Should().NotBeNull();
        result.Status.Should().Be(TournamentStatus.NotStarted);
        ctx.Tournaments.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_ReturnsExisting_WhenTournamentExists()
    {
        using var ctx = DbContextFactory.Create();
        var existing = new Tournament { Status = TournamentStatus.InProgress };
        ctx.Tournaments.Add(existing);
        await ctx.SaveChangesAsync();
        var sut = new TournamentService(ctx);

        var result = await sut.GetOrCreateAsync();

        result.Id.Should().Be(existing.Id);
        ctx.Tournaments.Should().HaveCount(1);
    }

    // ── RandomiseAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RandomiseAsync_Throws_WhenFewerThan2Teams()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 1);
        var sut = new TournamentService(ctx);

        var act = () => sut.RandomiseAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*2 teams*");
    }

    [Fact]
    public async Task RandomiseAsync_Throws_WhenTournamentAlreadyStarted()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        ctx.Tournaments.Add(new Tournament { Status = TournamentStatus.InProgress });
        await ctx.SaveChangesAsync();
        var sut = new TournamentService(ctx);

        var act = () => sut.RandomiseAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already started*");
    }

    [Fact]
    public async Task RandomiseAsync_CreatesCorrectMatchCount_ForEvenTeams()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);

        var result = await sut.RandomiseAsync();

        // 4 teams → 2 matches in round 1
        result.Matches.Should().HaveCount(2);
        result.Matches.Should().AllSatisfy(m => m.RoundNumber.Should().Be(1));
    }

    [Fact]
    public async Task RandomiseAsync_CreatesByeMatch_ForOddTeams()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 3);
        var sut = new TournamentService(ctx);

        var result = await sut.RandomiseAsync();

        // 3 teams → 1 normal match + 1 bye match
        result.Matches.Should().HaveCount(2);
        var byeMatch = result.Matches.FirstOrDefault(m => m.TeamBId == null);
        byeMatch.Should().NotBeNull("there should be a bye match for odd team count");
        byeMatch!.Status.Should().Be(MatchStatus.Completed);
        byeMatch.WinnerId.Should().NotBeNull();
    }

    [Fact]
    public async Task RandomiseAsync_ReplacesExistingMatches_WhenRerandomising()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);

        await sut.RandomiseAsync();
        var result = await sut.RandomiseAsync();

        // Still only 2 matches (old ones removed)
        result.Matches.Should().HaveCount(2);
        ctx.Matches.Should().HaveCount(2);
    }

    [Fact]
    public async Task RandomiseAsync_AssignsAllTeams_ForEvenTeams()
    {
        using var ctx = DbContextFactory.Create();
        var teams = await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);

        var result = await sut.RandomiseAsync();

        var usedTeamIds = result.Matches
            .SelectMany(m => new[] { m.TeamAId, m.TeamBId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        usedTeamIds.Should().BeEquivalentTo(teams.Select(t => t.Id));
    }

    // ── StartAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_Throws_WhenNoTournamentExists()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var act = () => sut.StartAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No tournament*");
    }

    [Fact]
    public async Task StartAsync_Throws_WhenNoMatchesDrawn()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Tournaments.Add(new Tournament());
        await ctx.SaveChangesAsync();
        var sut = new TournamentService(ctx);

        var act = () => sut.StartAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No matches*");
    }

    [Fact]
    public async Task StartAsync_Throws_WhenTournamentAlreadyStarted()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        await sut.RandomiseAsync();
        await sut.StartAsync();

        var act = () => sut.StartAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already started*");
    }

    [Fact]
    public async Task StartAsync_SetsStatusToInProgress()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        await sut.RandomiseAsync();

        var result = await sut.StartAsync();

        result.Status.Should().Be(TournamentStatus.InProgress);
    }

    // ── GetMatchesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetMatchesAsync_ReturnsEmpty_WhenNoTournament()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var result = await sut.GetMatchesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMatchesAsync_ReturnsMatches_OrderedByRoundAndSlot()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);
        await sut.RandomiseAsync();

        var result = (await sut.GetMatchesAsync()).ToList();

        result.Should().HaveCount(2);
        result.Should().BeInAscendingOrder(m => m.RoundNumber);
    }

    // ── StartMatchAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task StartMatchAsync_Throws_WhenMatchNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var act = () => sut.StartMatchAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Match 999 not found*");
    }

    [Fact]
    public async Task StartMatchAsync_Throws_WhenMatchAlreadyCompleted()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 3);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();

        // The bye match is already completed
        var byeMatch = tournament.Matches.First(m => m.Status == MatchStatus.Completed);

        var act = () => sut.StartMatchAsync(byeMatch.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public async Task StartMatchAsync_CreatesRound_AndSetsInProgress()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);

        var result = await sut.StartMatchAsync(match.Id);

        result.Status.Should().Be(MatchStatus.InProgress);
        result.RoundId.Should().NotBeNull();
    }

    [Fact]
    public async Task StartMatchAsync_IsIdempotent_WhenCalledTwice()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);
        await sut.StartMatchAsync(match.Id);

        // Calling again should not throw (returns the same match)
        var result = await sut.StartMatchAsync(match.Id);
        result.Status.Should().Be(MatchStatus.InProgress);
    }

    // ── CompleteMatchAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CompleteMatchAsync_Throws_WhenMatchNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var act = () => sut.CompleteMatchAsync(999);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Match 999 not found*");
    }

    [Fact]
    public async Task CompleteMatchAsync_Throws_WhenMatchAlreadyCompleted()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 3);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var byeMatch = tournament.Matches.First(m => m.Status == MatchStatus.Completed);

        var act = () => sut.CompleteMatchAsync(byeMatch.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public async Task CompleteMatchAsync_SetsWinnerToTeamA_WhenNoScores()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);
        await sut.StartMatchAsync(match.Id);

        var result = await sut.CompleteMatchAsync(match.Id);

        result.Status.Should().Be(MatchStatus.Completed);
        result.WinnerId.Should().Be(match.TeamAId); // TeamA wins when scores are tied at 0
    }

    [Fact]
    public async Task CompleteMatchAsync_SetsWinnerToHigherScoringTeam()
    {
        using var ctx = DbContextFactory.Create();
        var teams = await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();

        // Find the match and ensure teamA/teamB ordering
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);
        var started = await sut.StartMatchAsync(match.Id);

        // Add scores: TeamB scores more
        var objective = new Objective { Name = "Kill", Points = 100 };
        ctx.Objectives.Add(objective);
        await ctx.SaveChangesAsync();
        ctx.Scores.AddRange(
            new Score { TeamId = started.TeamAId!.Value, RoundId = started.RoundId!.Value, ObjectiveId = objective.Id, Points = 50 },
            new Score { TeamId = started.TeamBId!.Value, RoundId = started.RoundId!.Value, ObjectiveId = objective.Id, Points = 200 }
        );
        await ctx.SaveChangesAsync();

        var result = await sut.CompleteMatchAsync(match.Id);

        result.WinnerId.Should().Be(started.TeamBId);
    }

    // ── ResetAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetAsync_RemovesAllTournamentsAndMatches()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);
        await sut.RandomiseAsync();

        await sut.ResetAsync();

        ctx.Tournaments.Should().BeEmpty();
        ctx.Matches.Should().BeEmpty();
    }

    [Fact]
    public async Task ResetAsync_DoesNotThrow_WhenNothingExists()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var act = () => sut.ResetAsync();

        await act.Should().NotThrowAsync();
    }

    // ── FinaliseTournamentAsync ────────────────────────────────────────────

    [Fact]
    public async Task FinaliseTournamentAsync_Throws_WhenNoActiveTournament()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var act = () => sut.FinaliseTournamentAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No active tournament*");
    }

    [Fact]
    public async Task FinaliseTournamentAsync_Throws_WhenTournamentNotInProgress()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Tournaments.Add(new Tournament { Status = TournamentStatus.NotStarted });
        await ctx.SaveChangesAsync();
        var sut = new TournamentService(ctx);

        var act = () => sut.FinaliseTournamentAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in progress*");
    }

    [Fact]
    public async Task FinaliseTournamentAsync_Throws_WhenFinalNotComplete()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        await sut.RandomiseAsync();
        await sut.StartAsync();
        // Do NOT complete the match

        var act = () => sut.FinaliseTournamentAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*final match*");
    }

    [Fact]
    public async Task FinaliseTournamentAsync_SavesResult_AndClearsTournament()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);
        await sut.StartMatchAsync(match.Id);
        await sut.CompleteMatchAsync(match.Id);

        var result = await sut.FinaliseTournamentAsync();

        result.Should().NotBeNull();
        result.WinnerName.Should().NotBeNullOrEmpty();
        result.Entries.Should().NotBeEmpty();
        ctx.Tournaments.Should().BeEmpty();
        ctx.Matches.Should().BeEmpty();
        ctx.TournamentResults.Should().HaveCount(1);
    }

    [Fact]
    public async Task FinaliseTournamentAsync_IncludesAllTeamsInEntries()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.First(m => m.Status == MatchStatus.Pending);
        await sut.StartMatchAsync(match.Id);
        await sut.CompleteMatchAsync(match.Id);

        var result = await sut.FinaliseTournamentAsync();

        result.Entries.Should().HaveCount(2);
        result.Entries.Should().Contain(e => e.Position == 1);
        result.Entries.Should().Contain(e => e.Position == 2);
    }

    // ── GetResultsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetResultsAsync_ReturnsEmpty_WhenNoResults()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TournamentService(ctx);

        var result = await sut.GetResultsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetResultsAsync_ReturnsMostRecentFirst()
    {
        using var ctx = DbContextFactory.Create();
        ctx.TournamentResults.AddRange(
            new TournamentResult { WinnerName = "A", DatePlayed = DateTime.UtcNow.AddDays(-2) },
            new TournamentResult { WinnerName = "B", DatePlayed = DateTime.UtcNow });
        await ctx.SaveChangesAsync();
        var sut = new TournamentService(ctx);

        var result = (await sut.GetResultsAsync()).ToList();

        result[0].WinnerName.Should().Be("B");
        result[1].WinnerName.Should().Be("A");
    }

    // ── Lazy round generation (2-round tournament) ─────────────────────────

    [Fact]
    public async Task CompleteMatch_GeneratesNextRound_WhenRound1Complete()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4); // 4 teams → 2 R1 matches → 1 R2 match
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();

        // Complete all round-1 matches
        foreach (var match in tournament.Matches.Where(m => m.Status == MatchStatus.Pending).ToList())
        {
            await sut.StartMatchAsync(match.Id);
            await sut.CompleteMatchAsync(match.Id);
        }

        // Round 2 should now have been generated
        ctx.Matches.Should().Contain(m => m.RoundNumber == 2);
    }

    [Fact]
    public async Task FullTournament_2Teams_HasWinnerAfterOneMatch()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 2);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();
        var match = tournament.Matches.Single(m => m.Status == MatchStatus.Pending);
        await sut.StartMatchAsync(match.Id);
        var completed = await sut.CompleteMatchAsync(match.Id);

        completed.WinnerId.Should().NotBeNull();
        // No round 2 should be generated (the final is done)
        ctx.Matches.Count(m => m.RoundNumber == 2).Should().Be(0);
    }

    [Fact]
    public async Task FullTournament_5Teams_CreatesQualifierPlayoff()
    {
        // 5 teams → round 1: 2 normal matches + 1 bye = 3 winners (odd) → qualifier needed
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 5);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();

        // 2 normal matches + 1 bye in round 1
        tournament.Matches.Should().HaveCount(3);

        // Complete all pending round-1 matches
        var pending = tournament.Matches.Where(m => m.Status == MatchStatus.Pending).ToList();
        foreach (var m in pending)
        {
            await sut.StartMatchAsync(m.Id);
            await sut.CompleteMatchAsync(m.Id);
        }

        // After completing round 1 a qualifier playoff should have been created (IsPlayoff = true)
        var allMatches = await sut.GetMatchesAsync();
        allMatches.Should().Contain(m => m.IsPlayoff,
            "3 round-1 winners is odd, so a qualifier playoff must be generated");
    }

    [Fact]
    public async Task FullTournament_4Teams_ProducesChampion()
    {
        using var ctx = DbContextFactory.Create();
        await SeedTeamsAsync(ctx, 4);
        var sut = new TournamentService(ctx);
        var tournament = await sut.RandomiseAsync();
        await sut.StartAsync();

        // Round 1: 2 matches
        var r1 = tournament.Matches.Where(m => m.Status == MatchStatus.Pending).ToList();
        r1.Should().HaveCount(2);

        foreach (var m in r1)
        {
            await sut.StartMatchAsync(m.Id);
            await sut.CompleteMatchAsync(m.Id);
        }

        // Round 2 should have been generated: 1 final match
        var allMatches = (await sut.GetMatchesAsync()).ToList();
        var r2 = allMatches.Where(m => m.RoundNumber == 2).ToList();
        r2.Should().HaveCount(1);
        var final = r2.Single();
        await sut.StartMatchAsync(final.Id);
        var completed = await sut.CompleteMatchAsync(final.Id);

        completed.WinnerId.Should().NotBeNull();

        // Finalise — should succeed
        var result = await sut.FinaliseTournamentAsync();
        result.WinnerName.Should().NotBeNullOrEmpty();
    }
}
