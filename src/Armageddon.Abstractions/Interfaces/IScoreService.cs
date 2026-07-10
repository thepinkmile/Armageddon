using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface IScoreService
{
    Task<IEnumerable<Score>> GetScoresAsync();
    Task<IEnumerable<Score>> GetScoresByRoundAsync(int roundId);
    Task<IEnumerable<Score>> GetScoresByTeamAsync(int teamId);
    Task<Score> AddScoreAsync(int teamId, int roundId, int objectiveId, int points);
    Task<bool> RemoveScoreAsync(int id);
    Task<IEnumerable<Round>> GetRoundsAsync();
    Task<Round> AddRoundAsync(int number);
}
