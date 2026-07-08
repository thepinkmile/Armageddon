using System.Net.Http.Json;
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

    public async Task<Objective> AddObjectiveAsync(string name, ObjectiveType type = ObjectiveType.Recurring, int? maxUsage = null)
    {
        var response = await http.PostAsJsonAsync("api/objectives",
            new { Name = name, Type = type, MaxUsage = maxUsage });
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
}
