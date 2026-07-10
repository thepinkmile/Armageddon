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
    Task<Objective> AddObjectiveAsync(string name, int points = 100, int? maxUsage = null);
    Task<Objective> UpdateObjectiveAsync(int id, string name, int points, int? maxUsage);
    Task RemoveObjectiveAsync(int id);

    // Rounds
    Task<IEnumerable<Round>> GetRoundsAsync();
    Task<Round> AddRoundAsync(int number);

    // Scores
    Task<IEnumerable<Score>> GetScoresAsync();
    Task<Score> AddScoreAsync(int teamId, int roundId, int objectiveId, int points);
    Task RemoveScoreAsync(int id);

    // Tournament
    Task<Tournament> GetTournamentAsync();
    Task<Tournament> RandomiseTournamentAsync();
    Task<Tournament> StartTournamentAsync();
    Task<IEnumerable<Match>> GetMatchesAsync();
    Task<Match> StartMatchAsync(int matchId);
    Task<Match> CompleteMatchAsync(int matchId);
    Task<TournamentResult> FinaliseTournamentAsync();
    Task<IEnumerable<TournamentResult>> GetTournamentResultsAsync();
    Task ResetTournamentAsync();

    // Authentication / user management
    Task<string?> LoginAsync(string identifier, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);

    // Users (admin)
    Task<IEnumerable<User>> GetUsersAsync();
    Task<User> CreateUserAsync(CreateUserRequest req);
    Task DeleteUserAsync(string id);

    // Roles (admin)
    Task<IEnumerable<Role>> GetRolesAsync();
    Task<Role> CreateRoleAsync(CreateRoleRequest req);
    Task DeleteRoleAsync(string id);
}

public record CreateUserRequest(string UserName, string? Email, string Password, bool MustChangePassword);
public record CreateRoleRequest(string Name);
