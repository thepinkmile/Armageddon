using Armageddon.Abstractions.Models;

namespace Armageddon.Web.Services;

public class ArmageddonApiClient(HttpClient http) : IArmageddonApiClient
{
    public async Task<IEnumerable<Team>> GetTeamsAsync()
        => await http.GetFromJsonAsync<IEnumerable<Team>>("api/teams") ?? [];

    public async Task<Team> AddTeamAsync(string name)
    {
        var response = await http.PostAsJsonAsync("api/teams", name);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Team>())!;
    }

    public async Task RemoveTeamAsync(int id)
    {
        var response = await http.DeleteAsync($"api/teams/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Objective>> GetObjectivesAsync()
        => await http.GetFromJsonAsync<IEnumerable<Objective>>("api/objectives") ?? [];

    public async Task<Objective> AddObjectiveAsync(string name, int points = 100, int? maxUsage = null)
    {
        var response = await http.PostAsJsonAsync("api/objectives",
            new { Name = name, Points = points, MaxUsage = maxUsage });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Objective>())!;
    }

    public async Task<Objective> UpdateObjectiveAsync(int id, string name, int points, int? maxUsage)
    {
        var response = await http.PutAsJsonAsync($"api/objectives/{id}",
            new { Name = name, Points = points, MaxUsage = maxUsage });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Objective>())!;
    }

    public async Task RemoveObjectiveAsync(int id)
    {
        var response = await http.DeleteAsync($"api/objectives/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<Round>> GetRoundsAsync()
        => await http.GetFromJsonAsync<IEnumerable<Round>>("api/scores/rounds") ?? [];

    public async Task<Round> AddRoundAsync(int number)
    {
        var response = await http.PostAsJsonAsync("api/scores/rounds", number);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Round>())!;
    }

    public async Task<IEnumerable<Score>> GetScoresAsync()
        => await http.GetFromJsonAsync<IEnumerable<Score>>("api/scores") ?? [];

    public async Task<Score> AddScoreAsync(int teamId, int roundId, int objectiveId, int points)
    {
        var response = await http.PostAsJsonAsync("api/scores",
            new { TeamId = teamId, RoundId = roundId, ObjectiveId = objectiveId, Points = points });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Score>())!;
    }

    public async Task RemoveScoreAsync(int id)
    {
        var response = await http.DeleteAsync($"api/scores/{id}");
        response.EnsureSuccessStatusCode();
    }

    // ── Tournament ───────────────────────────────────────────────────────────

    public async Task<Tournament> GetTournamentAsync()
    {
        var resp = await http.GetFromJsonAsync<TournamentDto>("api/tournament");
        return MapTournament(resp!);
    }

    public async Task<Tournament> RandomiseTournamentAsync()
    {
        var response = await http.PostAsync("api/tournament/randomise", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>();
        return MapTournament(dto!);
    }

    public async Task<Tournament> StartTournamentAsync()
    {
        var response = await http.PostAsync("api/tournament/start", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TournamentDto>();
        return MapTournament(dto!);
    }

    public async Task<IEnumerable<Match>> GetMatchesAsync()
    {
        var dtos = await http.GetFromJsonAsync<IEnumerable<MatchDto>>("api/tournament/matches") ?? [];
        return dtos.Select(MapMatch).ToList();
    }

    public async Task<Match> StartMatchAsync(int matchId)
    {
        var response = await http.PostAsync($"api/tournament/matches/{matchId}/start", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<MatchDto>();
        return MapMatch(dto!);
    }

    public async Task<Match> CompleteMatchAsync(int matchId)
    {
        var response = await http.PostAsync($"api/tournament/matches/{matchId}/complete", null);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<MatchDto>();
        return MapMatch(dto!);
    }

    public async Task<TournamentResult> FinaliseTournamentAsync()
    {
        var response = await http.PostAsync("api/tournament/finalise", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TournamentResult>())!;
    }

    public async Task<IEnumerable<TournamentResult>> GetTournamentResultsAsync()
        => await http.GetFromJsonAsync<IEnumerable<TournamentResult>>("api/tournament/results") ?? [];

    public async Task ResetTournamentAsync()
    {
        var response = await http.PostAsync("api/tournament/reset", null);
        response.EnsureSuccessStatusCode();
    }

    // ── Authentication ───────────────────────────────────────────────────────

    public async Task<string?> LoginAsync(string identifier, string password)
    {
        var resp = await http.PostAsJsonAsync("api/auth/login", new { Identifier = identifier, Password = password });
        if (!resp.IsSuccessStatusCode) return null;
        var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        return data?.Token;
    }

    public async Task LogoutAsync()
    {
        await http.PostAsync("api/auth/logout", null);
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var resp = await http.GetAsync("api/auth/me");
        if (!resp.IsSuccessStatusCode) return null;
        var u = await resp.Content.ReadFromJsonAsync<UserResponse>();
        if (u is null) return null;
        return new User(Guid.Parse(u.Id), u.UserName, u.Email, u.MustChangePassword, true, u.Roles ?? []);
    }

    public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        var resp = await http.PostAsJsonAsync("api/auth/change-password", new { CurrentPassword = currentPassword, NewPassword = newPassword });
        return resp.IsSuccessStatusCode;
    }

    // ── Users (admin) ────────────────────────────────────────────────────────

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        var resp = await http.GetAsync("api/users");
        resp.EnsureSuccessStatusCode();
        var users = await resp.Content.ReadFromJsonAsync<IEnumerable<UserResponse>>() ?? [];
        return users.Select(u => new User(Guid.Parse(u.Id), u.UserName, u.Email, u.MustChangePassword, true, u.Roles ?? []));
    }

    public async Task<User> CreateUserAsync(CreateUserRequest req)
    {
        var resp = await http.PostAsJsonAsync("api/users", req);
        resp.EnsureSuccessStatusCode();
        var u = await resp.Content.ReadFromJsonAsync<UserResponse>();
        return new User(Guid.Parse(u!.Id), u.UserName, u.Email, u.MustChangePassword, true, u.Roles ?? []);
    }

    public async Task DeleteUserAsync(string id)
    {
        var resp = await http.DeleteAsync($"api/users/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Roles (admin) ────────────────────────────────────────────────────────

    public async Task<IEnumerable<Role>> GetRolesAsync()
    {
        var resp = await http.GetAsync("api/roles");
        resp.EnsureSuccessStatusCode();
        var roles = await resp.Content.ReadFromJsonAsync<IEnumerable<RoleResponse>>() ?? [];
        return roles.Select(r => new Role(Guid.Parse(r.Id), r.Name, true));
    }

    public async Task<Role> CreateRoleAsync(CreateRoleRequest req)
    {
        var resp = await http.PostAsJsonAsync("api/roles", req);
        resp.EnsureSuccessStatusCode();
        var r = await resp.Content.ReadFromJsonAsync<RoleResponse>();
        return new Role(Guid.Parse(r!.Id), r.Name, true);
    }

    public async Task DeleteRoleAsync(string id)
    {
        var resp = await http.DeleteAsync($"api/roles/{id}");
        resp.EnsureSuccessStatusCode();
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static Tournament MapTournament(TournamentDto dto) => new()
    {
        Id = dto.Id,
        Status = dto.Status,
        Matches = dto.Matches?.Select(MapMatch).ToList() ?? []
    };

    private static Match MapMatch(MatchDto dto) => new()
    {
        Id = dto.Id,
        RoundNumber = dto.RoundNumber,
        Slot = dto.Slot,
        TeamAId = dto.TeamAId,
        TeamA = dto.TeamA is not null ? new Team { Id = dto.TeamA.Id, Name = dto.TeamA.Name } : null,
        TeamBId = dto.TeamBId,
        TeamB = dto.TeamB is not null ? new Team { Id = dto.TeamB.Id, Name = dto.TeamB.Name } : null,
        WinnerId = dto.WinnerId,
        Winner = dto.Winner is not null ? new Team { Id = dto.Winner.Id, Name = dto.Winner.Name } : null,
        RoundId = dto.RoundId,
        Status = dto.Status,
        IsPlayoff = dto.IsPlayoff,
        TournamentId = dto.TournamentId
    };

    // ── Private response types ────────────────────────────────────────────────

    private record LoginResponse(string Token);
    private record UserResponse(string Id, string UserName, string? Email, bool MustChangePassword, string[] Roles);
    private record RoleResponse(string Id, string Name);

    private record TeamDto(int Id, string Name);
    private record MatchDto(
        int Id, int RoundNumber, int Slot, int TournamentId,
        int? TeamAId, TeamDto? TeamA,
        int? TeamBId, TeamDto? TeamB,
        int? WinnerId, TeamDto? Winner,
        int? RoundId, MatchStatus Status, bool IsPlayoff);
    private record TournamentDto(int Id, TournamentStatus Status, List<MatchDto>? Matches);
}
