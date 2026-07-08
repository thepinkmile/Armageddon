using Armageddon.Abstractions.Models;

namespace Armageddon.Web.Services;

public interface IArmageddonApiClient
{
    // Teams
    Task<IEnumerable<Team>> GetTeamsAsync();
    Task<Team> AddTeamAsync(string name);
    Task RemoveTeamAsync(int id);

    // Objectives
    Task<IEnumerable<Objective>> GetObjectivesAsync();
    Task<Objective> AddObjectiveAsync(string name, ObjectiveType type = ObjectiveType.Recurring, int? maxUsage = null);
    Task RemoveObjectiveAsync(int id);

    // Rounds
    Task<IEnumerable<Round>> GetRoundsAsync();
    Task<Round> AddRoundAsync(int number);

    // Scores
    Task<IEnumerable<Score>> GetScoresAsync();
    Task<Score> AddScoreAsync(int teamId, int roundId, int objectiveId, int points);
    Task RemoveScoreAsync(int id);
}
