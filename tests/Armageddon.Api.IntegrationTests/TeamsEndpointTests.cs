using System.Net;
using System.Net.Http.Json;
using Armageddon.Abstractions.Models;
using FluentAssertions;

namespace Armageddon.Api.IntegrationTests;

/// <summary>Black-box CRUD tests for /api/teams.</summary>
public class TeamsEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TeamsEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET all    // ── GET all ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeams_Returns200_WithList()
    {
        // Teams controller has no [Authorize] — publicly readable
        var response = await _client.GetAsync("/api/teams");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<List<Team>>();
        teams.Should().NotBeNull();
    }

    // ── POST (requires auth) ─────────────────────────────────────────────

    [Fact]
    public async Task PostTeam_Returns201_AndGetTeam_Returns200()
    {
        await _client.AuthenticateAsAdminAsync();

        // Controller takes [FromBody] string — send the raw JSON string value
        var createResponse = await _client.PostAsJsonAsync("/api/teams", "Team Alpha");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Team>();
        created!.Name.Should().Be("Team Alpha");
        created.Id.Should().BeGreaterThan(0);

        var getResponse = await _client.GetAsync($"/api/teams/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<Team>();
        fetched!.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task PostTeam_Returns400_WhenNameBlank()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PostAsJsonAsync("/api/teams", "");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTeam_Returns404_WhenNotFound()
    {
        var response = await _client.GetAsync("/api/teams/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteTeam_Returns204_ThenGet_Returns404()
    {
        await _client.AuthenticateAsAdminAsync();

        // Create a team within this test so we own its lifetime
        var createResponse = await _client.PostAsJsonAsync("/api/teams", "Team ToDelete");
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<Team>();

        var deleteResponse = await _client.DeleteAsync($"/api/teams/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/teams/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTeam_Returns404_WhenNotFound()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.DeleteAsync("/api/teams/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
