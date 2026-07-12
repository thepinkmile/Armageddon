using System.Net;
using System.Net.Http.Json;
using Armageddon.Abstractions.Models;
using FluentAssertions;

namespace Armageddon.Api.IntegrationTests;

/// <summary>Black-box CRUD tests for /api/objectives.</summary>
public class ObjectivesEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ObjectivesEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record CreateObjectiveRequest(string Name, int Points = 100, int? MaxUsage = null);
    private record UpdateObjectiveRequest(string Name, int Points, int? MaxUsage);

    [Fact]
    public async Task GetObjectives_Returns200_WithList()
    {
        // Objectives controller has no [Authorize] — publicly readable
        var response = await _client.GetAsync("/api/objectives");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<Objective>>();
        list.Should().NotBeNull();
    }

    [Fact]
    public async Task PostObjective_Returns201_WithDefaults()
    {
        await _client.AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest("Kill"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var obj = await response.Content.ReadFromJsonAsync<Objective>();
        obj!.Name.Should().Be("Kill");
        obj.Points.Should().Be(100);
        obj.MaxUsage.Should().BeNull();
    }

    [Fact]
    public async Task PostObjective_Returns400_WhenNameBlank()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest(""));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PutObjective_Returns200_WhenUpdated()
    {
        await _client.AuthenticateAsAdminAsync();

        var create = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest("OldName", 50));
        var created = await create.Content.ReadFromJsonAsync<Objective>();

        var update = await _client.PutAsJsonAsync($"/api/objectives/{created!.Id}",
            new UpdateObjectiveRequest("NewName", 200, 3));

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<Objective>();
        updated!.Name.Should().Be("NewName");
        updated.Points.Should().Be(200);
        updated.MaxUsage.Should().Be(3);
    }

    [Fact]
    public async Task PutObjective_Returns404_WhenNotFound()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PutAsJsonAsync("/api/objectives/99999",
            new UpdateObjectiveRequest("X", 10, null));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteObjective_Returns204_WhenExists()
    {
        await _client.AuthenticateAsAdminAsync();

        var create = await _client.PostAsJsonAsync("/api/objectives",
            new CreateObjectiveRequest("ToDelete"));
        var created = await create.Content.ReadFromJsonAsync<Objective>();

        var del = await _client.DeleteAsync($"/api/objectives/{created!.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteObjective_Returns404_WhenNotFound()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.DeleteAsync("/api/objectives/99999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
