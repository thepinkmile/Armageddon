using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using Armageddon.Api;

namespace Armageddon.Tests.Integration;

public class ApiLoginIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiLoginIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_Login_ReturnsJwt_WhenCredentialsValid()
    {
        var client = _factory.CreateClient();

        // Try login with seeded admin user created by the API startup (admin/admin)
        var response = await client.PostAsJsonAsync("api/auth/login", new { Identifier = "admin", Password = "admin" });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            // Fail with server response body to aid debugging
            throw new Xunit.Sdk.XunitException($"Login failed with {(int)response.StatusCode}: {body}");
        }
        var content = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(content);
    }
}
