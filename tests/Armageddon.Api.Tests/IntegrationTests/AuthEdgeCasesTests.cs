using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Armageddon.Api.Tests.IntegrationTests;

public class AuthEdgeCasesTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthEdgeCasesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Ensure test DB is seeded before creating client
        await _factory.EnsureTestDataAsync();
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new { Identifier = "no-such-user", Password = "bad" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_ReturnsJwt_WithMustChangeClaim_ForSeededAdmin()
    {
        await _factory.EnsureTestDataAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Identifier = "admin", Password = "admin" });
        var loginBody = await login.Content.ReadAsStringAsync();
        login.IsSuccessStatusCode.Should().BeTrue(loginBody);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        body.Should().NotBeNull();

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(body!.Token);
        token.Claims.Any(c => c.Type == "must_change_password" && c.Value == "true").Should().BeTrue();
    }

    private record LoginResponse(string Token);
}
