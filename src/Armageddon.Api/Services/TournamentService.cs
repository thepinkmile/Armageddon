using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Services;

public class TournamentService(ArmageddonDbContext context) : ITournamentService
{
    // ── Query helper ──────────────────────────────────────────────────────────

    private IQueryable<Tournament> TournamentsWithMatches()
        => context.Tournaments
            .Include(t => t.Matches).ThenInclude(m => m.TeamA)
            .Include(t => t.Matches).ThenInclude(m => m.TeamB)
            .Include(t => t.Matches).ThenInclude(m => m.Winner)
            .Include(t => t.Matches).ThenInclude(m => m.Round);

    // ── ITournamentService ────────────────────────────────────────────────────

    public async Task<Tournament> GetOrCreateAsync()
    {
        var tournament = await TournamentsWithMatches().FirstOrDefaultAsync();
        if (tournament is null)
        {
            tournament = new Tournament();
            context.Tournaments.Add(tournament);
            await context.SaveChangesAsync();
        }
        return tournament;
    }

    public async Task<Tournament> RandomiseAsync()
    {
        var tournament = await TournamentsWithMatches().FirstOrDefaultAsync();

        if (tournament is not null && tournament.Status != TournamentStatus.NotStarted)
            throw new InvalidOperationException("Cannot randomise a tournament that has already started.");

        var teams = await context.Teams.ToListAsync();
        if (teams.Count < 2)
            throw new InvalidOperationException("At least 2 teams are required.");

        // Remove existing matches if re-randomising
        if (tournament is not null)
        {
            context.Matches.RemoveRange(tournament.Matches);
            await context.SaveChangesAsync();
        }
        else
        {
            tournament = new Tournament();
            context.Tournaments.Add(tournament);
            await context.SaveChangesAsync();
        }

        // Shuffle teams
        var shuffled = teams.OrderBy(_ => Random.Shared.Next()).ToList();

        // Build round-1 matches only. Rounds 2+ are generated lazily after each round completes.
        // If there is an odd number of teams the last team gets a bye (auto-wins round 1).
        var round1Matches = new List<Match>();
        int pairs = shuffled.Count / 2;
        bool hasBye = shuffled.Count % 2 == 1;

        for (int slot = 0; slot < pairs; slot++)
        {
            round1Matches.Add(new Match
            {
                TournamentId = tournament.Id,
                RoundNumber  = 1,
                Slot         = slot,
                TeamAId      = shuffled[slot * 2].Id,
                TeamBId      = shuffled[slot * 2 + 1].Id,
                Status       = MatchStatus.Pending
            });
        }

        if (hasBye)
        {
            // The last team gets a bye — auto-completed with that team as winner
            var byeTeam = shuffled[^1];
            round1Matches.Add(new Match
            {
                TournamentId = tournament.Id,
                RoundNumber  = 1,
                Slot         = pairs,
                TeamAId      = byeTeam.Id,
                TeamBId      = null,
                WinnerId     = byeTeam.Id,
                Status       = MatchStatus.Completed
            });
        }

        context.Matches.AddRange(round1Matches);
        await context.SaveChangesAsync();

        return await TournamentsWithMatches().FirstAsync(t => t.Id == tournament.Id);
    }

    public async Task<Tournament> StartAsync()
    {
        var tournament = await TournamentsWithMatches().FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No tournament exists. Randomise first.");

        if (tournament.Status != TournamentStatus.NotStarted)
            throw new InvalidOperationException("Tournament has already started.");

        if (!tournament.Matches.Any())
            throw new InvalidOperationException("No matches have been drawn. Randomise first.");

        tournament.Status = TournamentStatus.InProgress;
        await context.SaveChangesAsync();

        return await TournamentsWithMatches().FirstAsync(t => t.Id == tournament.Id);
    }

    public async Task<IEnumerable<Match>> GetMatchesAsync()
    {
        var tournament = await TournamentsWithMatches().FirstOrDefaultAsync();
        return tournament?.Matches
            .OrderBy(m => m.RoundNumber).ThenBy(m => m.IsPlayoff ? 1 : 0).ThenBy(m => m.Slot)
            .ToList() ?? [];
    }

    public async Task<Match> StartMatchAsync(int matchId)
    {
        var match = await context.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException($"Match {matchId} not found.");

        if (match.Status == MatchStatus.Completed)
            throw new InvalidOperationException("Match is already completed.");

        if (!match.RoundId.HasValue)
        {
            var maxRoundNumber = await context.Rounds.AnyAsync()
                ? await context.Rounds.MaxAsync(r => r.Number)
                : 0;
            var round = new Round { Number = maxRoundNumber + 1 };
            context.Rounds.Add(round);
            await context.SaveChangesAsync();
            match.RoundId = round.Id;
            match.Status  = MatchStatus.InProgress;
            await context.SaveChangesAsync();
        }

        return await context.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .Include(m => m.Round)
            .FirstAsync(m => m.Id == matchId);
    }

    public async Task<Match> CompleteMatchAsync(int matchId)
    {
        var match = await context.Matches
            .Include(m => m.Round).ThenInclude(r => r!.Scores)
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .FirstOrDefaultAsync(m => m.Id == matchId)
            ?? throw new InvalidOperationException($"Match {matchId} not found.");

        if (match.Status == MatchStatus.Completed)
            throw new InvalidOperationException("Match is already completed.");

        int scoreA = match.RoundId.HasValue
            ? match.Round!.Scores.Where(s => s.TeamId == match.TeamAId).Sum(s => s.Points) : 0;
        int scoreB = match.RoundId.HasValue
            ? match.Round!.Scores.Where(s => s.TeamId == match.TeamBId).Sum(s => s.Points) : 0;

        match.WinnerId = (scoreB > scoreA && match.TeamBId.HasValue) ? match.TeamBId : match.TeamAId;
        match.Status   = MatchStatus.Completed;
        await context.SaveChangesAsync();

        // Lazily generate / update the next round after this match completes
        await TryAdvanceRoundAsync(match.TournamentId, match.RoundNumber, match.IsPlayoff);

        return await context.Matches
            .Include(m => m.TeamA)
            .Include(m => m.TeamB)
            .Include(m => m.Winner)
            .FirstAsync(m => m.Id == matchId);
    }

    public async Task ResetAsync()
    {
        var tournaments = await TournamentsWithMatches().ToListAsync();
        foreach (var t in tournaments)
            context.Matches.RemoveRange(t.Matches);
        context.Tournaments.RemoveRange(tournaments);
        await context.SaveChangesAsync();
    }

    public async Task<TournamentResult> FinaliseTournamentAsync()
    {
        var tournament = await TournamentsWithMatches().FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("No active tournament.");

        if (tournament.Status != TournamentStatus.InProgress)
            throw new InvalidOperationException("Tournament is not in progress.");

        var allMatches = tournament.Matches
            .Where(m => !m.IsPlayoff)
            .OrderBy(m => m.RoundNumber).ThenBy(m => m.Slot).ToList();

        // Include playoff matches for points tallying (scores count toward total)
        var allMatchesIncPlayoff = tournament.Matches
            .OrderBy(m => m.RoundNumber).ThenBy(m => m.Slot).ToList();

        // Load scores separately to avoid circular reference explosion in the general query
        var allRoundIds = allMatchesIncPlayoff.Where(m => m.RoundId.HasValue).Select(m => m.RoundId!.Value).Distinct().ToList();
        var scoresByRound = await context.Scores
            .Where(s => allRoundIds.Contains(s.RoundId))
            .ToListAsync();

        int totalRounds = allMatches.Select(m => m.RoundNumber).DefaultIfEmpty(0).Max();
        var finalMatch  = allMatches.FirstOrDefault(m => m.RoundNumber == totalRounds && m.Status == MatchStatus.Completed);
        if (finalMatch?.Winner == null)
            throw new InvalidOperationException("The final match has not been completed yet.");

        var teamInfo = new Dictionary<int, (string Name, int RoundReached)>();
        foreach (var m in allMatches.Where(m => m.Status == MatchStatus.Completed))
        {
            void Track(int? id, string? name)
            {
                if (id is null || name is null) return;
                if (!teamInfo.TryGetValue(id.Value, out var existing) || m.RoundNumber > existing.RoundReached)
                    teamInfo[id.Value] = (name, m.RoundNumber);
            }
            Track(m.TeamAId, m.TeamA?.Name);
            Track(m.TeamBId, m.TeamB?.Name);
        }

        int winnerId  = finalMatch.Winner.Id;
        int? runnerId = finalMatch.TeamAId == winnerId ? finalMatch.TeamBId : finalMatch.TeamAId;

        static int RoundToPosition(int roundReached, int totalRounds)
        {
            int fromFinal = totalRounds - roundReached;
            return fromFinal switch { 0 => 2, 1 => 3, 2 => 5, _ => (int)Math.Pow(2, fromFinal) + 1 };
        }

        var entries = new List<TournamentResultEntry>();
        foreach (var (teamId, (name, roundReached)) in teamInfo)
        {
            int position = teamId == winnerId ? 1
                         : teamId == runnerId ? 2
                         : RoundToPosition(roundReached, totalRounds);

            // Sum all scores across every match (main + playoff) for this team
            int totalPoints = allMatchesIncPlayoff
                .Where(m => m.Status == MatchStatus.Completed && m.RoundId.HasValue
                         && (m.TeamAId == teamId || m.TeamBId == teamId))
                .Sum(m => scoresByRound.Where(s => s.RoundId == m.RoundId && s.TeamId == teamId).Sum(s => s.Points));

            entries.Add(new TournamentResultEntry
            {
                TeamName    = name,
                Position    = position,
                RoundReached = roundReached,
                TotalPoints = totalPoints
            });
        }

        var result = new TournamentResult
        {
            DatePlayed = DateTime.UtcNow,
            WinnerName = finalMatch.Winner.Name,
            Entries    = entries
        };
        context.TournamentResults.Add(result);
        context.Matches.RemoveRange(tournament.Matches);
        context.Tournaments.Remove(tournament);
        await context.SaveChangesAsync();
        return result;
    }

    public async Task<IEnumerable<TournamentResult>> GetResultsAsync()
        => await context.TournamentResults
            .Include(r => r.Entries)
            .OrderByDescending(r => r.DatePlayed)
            .ToListAsync();

    // ── Lazy round-advance logic ──────────────────────────────────────────────

    /// <summary>
    /// Called after every match completion. Checks whether the round is now fully
    /// done and, if so, either fills an open playoff slot or generates the next main round.
    /// </summary>
    private async Task TryAdvanceRoundAsync(int tournamentId, int roundNumber, bool completedMatchIsPlayoff)
    {
        // Reload all matches for this tournament (fresh, no tracking issues)
        var allMatches = await context.Matches
            .Include(m => m.Round).ThenInclude(r => r!.Scores)
            .Where(m => m.TournamentId == tournamentId)
            .ToListAsync();

        var mainMatches    = allMatches.Where(m => m.RoundNumber == roundNumber && !m.IsPlayoff).ToList();
        var playoffMatches = allMatches.Where(m => m.RoundNumber == roundNumber && m.IsPlayoff).ToList();

        bool mainAllDone    = mainMatches.All(m => m.Status == MatchStatus.Completed);
        bool playoffAllDone = !playoffMatches.Any() || playoffMatches.All(m => m.Status == MatchStatus.Completed);

        if (!mainAllDone) return; // Still matches to play in this round

        // ── Case A: Playoff just finished → fill the open slot in the next round ──
        if (completedMatchIsPlayoff && playoffAllDone && playoffMatches.Any())
        {
            await FillPlayoffSlotAsync(tournamentId, roundNumber, allMatches, playoffMatches);
            return;
        }

        // ── Case B: All main + playoff complete → generate the next round ──
        if (mainAllDone && playoffAllDone)
        {
            await GenerateNextRoundAsync(tournamentId, roundNumber, allMatches);
        }
    }

    /// <summary>
    /// After the playoff for a given main round is complete, pick the best playoff team
    /// and place them in the pending TBD slot of the already-created next round.
    /// </summary>
    private async Task FillPlayoffSlotAsync(
        int tournamentId,
        int roundNumber,
        List<Match> allMatches,
        List<Match> playoffMatches)
    {
        int? winnerId = DeterminePlayoffQualifier(playoffMatches);
        if (winnerId is null) return;

        // Find the next round's match that still has a TBD slot
        int nextRound = roundNumber + 1;
        var tbdMatch = allMatches
            .Where(m => m.RoundNumber == nextRound && !m.IsPlayoff)
            .FirstOrDefault(m => m.TeamAId is null || m.TeamBId is null);

        if (tbdMatch is null) return;

        if (tbdMatch.TeamAId is null)
            tbdMatch.TeamAId = winnerId;
        else
            tbdMatch.TeamBId = winnerId;

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Generates the next round's matches once the current round (including any playoffs)
    /// is fully complete. If the number of winners is odd, creates a playoff among the
    /// top-scoring non-winners to determine who fills the extra slot.
    /// </summary>
    private async Task GenerateNextRoundAsync(int tournamentId, int roundNumber, List<Match> allMatches)
    {
        // Collect winners from the current main round
        var mainMatches = allMatches
            .Where(m => m.RoundNumber == roundNumber && !m.IsPlayoff && m.Status == MatchStatus.Completed)
            .ToList();

        if (mainMatches.Count == 0) return;

        // If only one match in this round and it's complete with a winner → this was the final.
        // Do NOT mark the tournament completed here — that is the user's explicit action via
        // FinaliseTournamentAsync (the "End Tournament" button). Just stop generating rounds.
        if (mainMatches.Count == 1 && mainMatches[0].WinnerId.HasValue)
            return;

        var winnerIds = mainMatches
            .Where(m => m.WinnerId.HasValue)
            .Select(m => m.WinnerId!.Value)
            .Distinct()
            .ToList();

        if (winnerIds.Count <= 1) return; // Final complete; nothing to generate

        int nextRound       = roundNumber + 1;
        int nextPairs       = winnerIds.Count / 2;
        bool needsQualifier = winnerIds.Count % 2 == 1;

        // Already generated? (idempotency guard)
        if (allMatches.Any(m => m.RoundNumber == nextRound && !m.IsPlayoff)) return;

        // Shuffle winners before assigning
        var shuffled = winnerIds.OrderBy(_ => Random.Shared.Next()).ToList();

        var newMatches = new List<Match>();
        for (int slot = 0; slot < nextPairs; slot++)
        {
            newMatches.Add(new Match
            {
                TournamentId = tournamentId,
                RoundNumber  = nextRound,
                Slot         = slot,
                TeamAId      = shuffled[slot * 2],
                TeamBId      = shuffled[slot * 2 + 1],
                Status       = MatchStatus.Pending
            });
        }

        if (needsQualifier)
        {
            // One winner has no opponent yet — TBD until qualifier resolves
            newMatches.Add(new Match
            {
                TournamentId = tournamentId,
                RoundNumber  = nextRound,
                Slot         = nextPairs,
                TeamAId      = shuffled[^1], // the "bye" winner
                TeamBId      = null,         // filled by playoff winner or direct qualifier
                Status       = MatchStatus.Pending
            });
        }

        // Save all next-round matches FIRST so the TBD slot exists in the DB
        // before the qualifier logic tries to query/fill it.
        context.Matches.AddRange(newMatches);
        await context.SaveChangesAsync();

        if (needsQualifier)
        {
            // Now create round-robin qualifier (or directly fill TBD if only one top loser)
            await CreateQualifierPlayoffAsync(tournamentId, roundNumber, allMatches, shuffled);
        }
    }

    /// <summary>
    /// Identifies the best-scoring non-winner(s) from <paramref name="roundNumber"/>
    /// and creates a round-robin playoff (every pair plays once) among them.
    /// The playoff is marked with <c>IsPlayoff = true</c> and the same <c>RoundNumber</c>
    /// so it is visually associated with the current round.
    /// </summary>
    private async Task CreateQualifierPlayoffAsync(
        int tournamentId,
        int roundNumber,
        List<Match> allMatches,
        List<int> winnerIds)
    {
        // Collect all non-winning participants from the current round
        var losers = allMatches
            .Where(m => m.RoundNumber == roundNumber && !m.IsPlayoff && m.Status == MatchStatus.Completed)
            .SelectMany(m => new[]
            {
                m.TeamAId.HasValue && m.TeamAId != m.WinnerId ? m.TeamAId : null,
                m.TeamBId.HasValue && m.TeamBId != m.WinnerId ? m.TeamBId : null
            })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (losers.Count == 0) return;

        // Score each loser across their round matches
        var loserScores = new Dictionary<int, int>();
        foreach (var loser in losers)
        {
            int total = allMatches
                .Where(m => m.RoundNumber == roundNumber && !m.IsPlayoff && m.Status == MatchStatus.Completed
                         && (m.TeamAId == loser || m.TeamBId == loser) && m.RoundId.HasValue)
                .Sum(m =>
                {
                    var scores = m.Round?.Scores ?? Enumerable.Empty<Score>();
                    return scores.Where(s => s.TeamId == loser).Sum(s => s.Points);
                });
            loserScores[loser] = total;
        }

        // Find the maximum score among losers
        int maxScore    = loserScores.Values.DefaultIfEmpty(0).Max();
        var topLosers   = loserScores.Where(kv => kv.Value == maxScore).Select(kv => kv.Key).ToList();

        if (topLosers.Count == 0) return;

        // If only one top loser → they are the qualifier, no matches needed
        if (topLosers.Count == 1)
        {
            // Directly fill the TBD slot in the next round
            int nextRound = roundNumber + 1;
            var tbdMatch  = await context.Matches
                .Where(m => m.TournamentId == tournamentId && m.RoundNumber == nextRound && !m.IsPlayoff)
                .FirstOrDefaultAsync(m => m.TeamAId == null || m.TeamBId == null);
            if (tbdMatch != null)
            {
                if (tbdMatch.TeamAId is null) tbdMatch.TeamAId = topLosers[0];
                else tbdMatch.TeamBId = topLosers[0];
                await context.SaveChangesAsync();
            }
            return;
        }

        // Create round-robin: every pair plays once
        // Number of playoff slots = existing playoff match count in this round (to avoid re-index collisions)
        int slotBase  = allMatches.Count(m => m.RoundNumber == roundNumber && m.IsPlayoff);
        int slotIndex = slotBase;

        var playoffMatches = new List<Match>();
        for (int i = 0; i < topLosers.Count - 1; i++)
        {
            for (int j = i + 1; j < topLosers.Count; j++)
            {
                playoffMatches.Add(new Match
                {
                    TournamentId = tournamentId,
                    RoundNumber  = roundNumber,
                    Slot         = slotIndex++,
                    TeamAId      = topLosers[i],
                    TeamBId      = topLosers[j],
                    Status       = MatchStatus.Pending,
                    IsPlayoff    = true
                });
            }
        }

        context.Matches.AddRange(playoffMatches);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Determines the single qualifier from a set of round-robin playoff matches.
    /// Ranking: most wins → highest total points → lowest team ID.
    /// </summary>
    private static int? DeterminePlayoffQualifier(List<Match> playoffMatches)
    {
        var completed = playoffMatches.Where(m => m.Status == MatchStatus.Completed).ToList();
        if (!completed.Any()) return null;

        var teamIds = completed
            .SelectMany(m => new[] { m.TeamAId, m.TeamBId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var wins   = teamIds.ToDictionary(id => id, id => 0);
        var points = teamIds.ToDictionary(id => id, id => 0);

        foreach (var m in completed)
        {
            if (m.WinnerId.HasValue && wins.ContainsKey(m.WinnerId.Value))
                wins[m.WinnerId.Value]++;

            int scoreA = m.Round?.Scores.Where(s => s.TeamId == m.TeamAId).Sum(s => s.Points) ?? 0;
            int scoreB = m.Round?.Scores.Where(s => s.TeamId == m.TeamBId).Sum(s => s.Points) ?? 0;

            if (m.TeamAId.HasValue && points.ContainsKey(m.TeamAId.Value)) points[m.TeamAId.Value] += scoreA;
            if (m.TeamBId.HasValue && points.ContainsKey(m.TeamBId.Value)) points[m.TeamBId.Value] += scoreB;
        }

        return teamIds
            .OrderByDescending(id => wins[id])
            .ThenByDescending(id => points[id])
            .ThenBy(id => id)
            .FirstOrDefault();
    }
}


