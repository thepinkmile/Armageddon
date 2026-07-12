using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Armageddon.Api.IntegrationTests;

/// <summary>
/// Black-box tests for the /api/auth/* endpoints.
/// The API is hosted in-process via WebApplicationFactory with an in-memory database.
/// </summary>
public class AuthEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private record LoginRequest(string Identifier, string Password);
    private record LoginResponse(string Token);
    private record CurrentUserResponse(string Id, string UserName, string? Email, bool MustChangePassword, string[] Roles);

    // ── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Returns200_WithToken_WhenCredentialsValid()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "admin"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns401_WhenPasswordWrong()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "wrongpassword"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_Returns400_WhenCredentialsBlank()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("", ""));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_Returns401_WhenUserNotFound()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("nonexistent_user", "password"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Me ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_Returns401_WhenNotAuthenticated()
    {
        var response = await _client.GetAsync("/api/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_Returns200_WithUserInfo_WhenAuthenticated()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        body!.UserName.Should().Be("admin");
        body.Roles.Should().Contain("Admin");
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Returns200_WhenAuthenticated()
    {
        await _client.AuthenticateAsAdminAsync();
        var response = await _client.PostAsync("/api/auth/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── ChangePassword ────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Returns400_WhenCurrentPasswordWrong()
    {
        await _client.AuthenticateAsAdminAsync();

        var response = await _client.PostAsJsonAsync("/api/auth/change-password",
            new { CurrentPassword = "wrongpassword", NewPassword = "newpass" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
