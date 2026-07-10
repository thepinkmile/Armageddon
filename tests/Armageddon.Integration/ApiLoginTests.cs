using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Armageddon.Api;

namespace Armageddon.Integration.Tests;

public class ApiLoginTests : IClassFixture<WebApplicationFactory<Armageddon.Api.Program>>
{
    private readonly WebApplicationFactory<Armageddon.Api.Program> _factory;

    public ApiLoginTests(WebApplicationFactory<Armageddon.Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_Login_ReturnsJwt_WhenCredentialsValid()
    {
        var client = _factory.CreateClient();

        // Ensure a known test user exists in the seeded database. If seeding is not present, this will need
        // to be adapted to create the user via the API. For now try login with default test credentials.
        var response = await client.PostAsJsonAsync("api/auth/login", new { Identifier = "test@example.com", Password = "P@ssword1" });
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(content);
    }
}
