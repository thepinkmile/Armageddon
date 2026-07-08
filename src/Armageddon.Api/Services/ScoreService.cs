using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Services;

public class ScoreService(ArmageddonDbContext context) : IScoreService
{
    public async Task<IEnumerable<Score>> GetScoresAsync()
        => await context.Scores
            .AsNoTracking()
            .Include(s => s.Team)
            .Include(s => s.Round)
            .Include(s => s.Objective)
            .ToListAsync();

    public async Task<IEnumerable<Score>> GetScoresByRoundAsync(int roundId)
        => await context.Scores
            .AsNoTracking()
            .Include(s => s.Team)
            .Include(s => s.Round)
            .Include(s => s.Objective)
            .Where(s => s.RoundId == roundId)
            .ToListAsync();

    public async Task<IEnumerable<Score>> GetScoresByTeamAsync(int teamId)
        => await context.Scores
            .AsNoTracking()
            .Include(s => s.Team)
            .Include(s => s.Round)
            .Include(s => s.Objective)
            .Where(s => s.TeamId == teamId)
            .ToListAsync();

    public async Task<Score> AddScoreAsync(int teamId, int roundId, int objectiveId, int points)
    {
        var score = new Score
        {
            TeamId = teamId,
            RoundId = roundId,
            ObjectiveId = objectiveId,
            Points = points
        };
        context.Scores.Add(score);
        await context.SaveChangesAsync();
        return score;
    }

    public async Task<bool> RemoveScoreAsync(int id)
    {
        var score = await context.Scores.FindAsync(id);
        if (score is null) return false;
        context.Scores.Remove(score);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Round>> GetRoundsAsync()
        => await context.Rounds.AsNoTracking().OrderBy(r => r.Number).ToListAsync();

    public async Task<Round> AddRoundAsync(int number)
    {
        var round = new Round { Number = number };
        context.Rounds.Add(round);
        await context.SaveChangesAsync();
        return round;
    }
}
