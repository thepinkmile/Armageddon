using Armageddon.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Armageddon.Web.UnitTests;

public class ApiAuthenticationStateProviderTests
{
    // Build a minimal HS256 JWT with the supplied payload claims so tests do
    // not depend on any third-party JWT library at run-time.
    private static string BuildJwt(Dictionary<string, object> payload)
    {
        static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        var body   = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload)));
        var sig    = Base64UrlEncode(Encoding.UTF8.GetBytes("fakesig"));
        return $"{header}.{body}.{sig}";
    }

    // ── GetAuthenticationStateAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAuthenticationStateAsync_ReturnsAnonymous_ByDefault()
    {
        var sut = new ApiAuthenticationStateProvider();

        var state = await sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    // ── NotifyUserAuthentication ─────────────────────────────────────────────

    [Fact]
    public async Task NotifyUserAuthentication_SetsAuthenticatedUser()
    {
        var sut = new ApiAuthenticationStateProvider();
        var token = BuildJwt(new() { ["sub"] = "user1", ["name"] = "Alice" });

        sut.NotifyUserAuthentication(token);

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task NotifyUserAuthentication_ParsesSimpleClaim()
    {
        var sut = new ApiAuthenticationStateProvider();
        var token = BuildJwt(new() { ["sub"] = "user42" });

        sut.NotifyUserAuthentication(token);

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "user42");
    }

    [Fact]
    public async Task NotifyUserAuthentication_ParsesArrayClaim()
    {
        var sut = new ApiAuthenticationStateProvider();
        var token = BuildJwt(new()
        {
            ["role"] = new[] { "Admin", "User" }
        });

        sut.NotifyUserAuthentication(token);

        var state = await sut.GetAuthenticationStateAsync();
        var roles = state.User.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList();
        roles.Should().Contain("Admin").And.Contain("User");
    }

    [Fact]
    public async Task NotifyUserAuthentication_MultipleTokens_UsesLatest()
    {
        var sut = new ApiAuthenticationStateProvider();
        var first  = BuildJwt(new() { ["sub"] = "old" });
        var second = BuildJwt(new() { ["sub"] = "new" });

        sut.NotifyUserAuthentication(first);
        sut.NotifyUserAuthentication(second);

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "new");
        state.User.Claims.Should().NotContain(c => c.Type == "sub" && c.Value == "old");
    }

    [Fact]
    public async Task NotifyUserAuthentication_InvalidJwtFormat_AuthenticatedWithNoClaims()
    {
        // The implementation always authenticates (ClaimsIdentity has authenticationType "apiauth")
        // but returns no claims when the JWT does not have exactly 3 dot-separated parts.
        var sut = new ApiAuthenticationStateProvider();

        sut.NotifyUserAuthentication("not.a.valid.jwt.at.all.extra");

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeTrue();
        state.User.Claims.Should().BeEmpty();
    }

    // ── NotifyUserLogout ──────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyUserLogout_ClearsAuthenticatedUser()
    {
        var sut = new ApiAuthenticationStateProvider();
        var token = BuildJwt(new() { ["sub"] = "user1" });
        sut.NotifyUserAuthentication(token);

        sut.NotifyUserLogout();

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task NotifyUserLogout_WhenAlreadyAnonymous_RemainsAnonymous()
    {
        var sut = new ApiAuthenticationStateProvider();

        sut.NotifyUserLogout();

        var state = await sut.GetAuthenticationStateAsync();
        state.User.Identity!.IsAuthenticated.Should().BeFalse();
    }
}
