using System.Net;
using System.Net.Http.Json;
using Armageddon.Abstractions.Models;
using FluentAssertions;

namespace Armageddon.Api.IntegrationTests;

/// <summary>
/// Black-box tests for the tournament lifecycle over HTTP.
/// Each test class that needs an isolated DB state uses its own factory instance.
/// </summary>
public class TournamentEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TournamentEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task SeedTeamsAsync(int count)
    {
        await _client.AuthenticateAsAdminAsync();
        for (int i = 1; i <= count; i++)
            await _client.PostAsJsonAsync("/api/teams", $"Team {i}");
    }

    // ── GET tournament (no [Authorize] — publicly readable) ───────────────

    [Fact]
    public async Task GetTournament_Returns200()
    {
        var response = await _client.GetAsync("/api/tournament");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── Randomise ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Randomise_Returns200_WithMatches_WhenEnoughTeams()
    {
        await SeedTeamsAsync(4);
        var response = await _client.PostAsync("/api/tournament/randomise", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tournament = await response.Content.ReadFromJsonAsync<Tournament>();
        tournament!.Matches.Should().NotBeEmpty();
    }

    // ── Full lifecycle: start → play → finalise ───────────────────────────

    [Fact]
    public async Task FullLifecycle_RandomiseStartPlayFinalise_Returns200()
    {
        await SeedTeamsAsync(2);

        var randomise = await _client.PostAsync("/api/tournament/randomise", null);
        randomise.StatusCode.Should().Be(HttpStatusCode.OK);

        var start = await _client.PostAsync("/api/tournament/start", null);
        start.StatusCode.Should().Be(HttpStatusCode.OK);

        var matchesResponse = await _client.GetAsync("/api/tournament/matches");
        var matches = await matchesResponse.Content.ReadFromJsonAsync<List<Match>>();
        var pending = matches!.Where(m => m.Status == MatchStatus.Pending
                                       && m.TeamAId.HasValue
                                       && m.TeamBId.HasValue).ToList();

        // correct URL: /api/tournament/matches/{id}/start
        foreach (var match in pending)
        {
            var startMatch = await _client.PostAsync($"/api/tournament/matches/{match.Id}/start", null);
            startMatch.StatusCode.Should().Be(HttpStatusCode.OK);

            var completeMatch = await _client.PostAsync($"/api/tournament/matches/{match.Id}/complete", null);
            completeMatch.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var finalise = await _client.PostAsync("/api/tournament/finalise", null);
        finalise.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await finalise.Content.ReadFromJsonAsync<TournamentResult>();
        result!.Entries.Should().HaveCount(2);
        result.Entries.Should().Contain(e => e.Position == 1);

        var history = await _client.GetAsync("/api/tournament/results");
        history.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await history.Content.ReadFromJsonAsync<List<TournamentResult>>();
        results.Should().NotBeEmpty();
    }

    // ── Reset ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Reset_Returns204()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PostAsync("/api/tournament/reset", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ── Health ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HealthEndpoint_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

/// <summary>Isolated factory for the "randomise with no teams" scenario.</summary>
public class TournamentNoTeamsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TournamentNoTeamsTests(ApiWebApplicationFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Randomise_Returns400_WhenNoTeams()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PostAsync("/api/tournament/randomise", null);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
