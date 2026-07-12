#nullable enable
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Armageddon.Api.Tests.IntegrationTests;

public class AdminTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<string> GetAdminTokenAsync()
    {
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Identifier = "admin", Password = "admin" });
        var loginBodyText = await login.Content.ReadAsStringAsync();
        System.Console.WriteLine($"POST /api/auth/login => {(int)login.StatusCode} {login.StatusCode}\n{loginBodyText}");
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    [Fact]
    public async Task Admin_Can_List_Users()
    {
        await _factory.EnsureTestDataAsync();
        var client = _factory.CreateClient();
        // Login using this client so the same test server instance and handler are used
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Identifier = "admin", Password = "admin" });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginBody!.Token;
        System.Console.WriteLine($"Admin token: {token}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        // Diagnostic: verify token is accepted by /api/auth/me
        var me = await client.GetAsync("/api/auth/me");
        var meBody = await me.Content.ReadAsStringAsync();
        System.Console.WriteLine($"GET /api/auth/me => {(int)me.StatusCode} {me.StatusCode}\n{meBody}");

        var resp = await client.GetAsync("/api/users");
        var respBody = await resp.Content.ReadAsStringAsync();
        System.Console.WriteLine($"GET /api/users => {(int)resp.StatusCode} {resp.StatusCode}\n{respBody}");
        resp.IsSuccessStatusCode.Should().BeTrue(respBody);
        var users = await resp.Content.ReadFromJsonAsync<UserDto[]>();
        users.Should().NotBeNull();
        users!.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_Can_List_Roles()
    {
        await _factory.EnsureTestDataAsync();
        var client = _factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { Identifier = "admin", Password = "admin" });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<LoginResponse>();
        var token = loginBody!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/api/roles");
        var respBody = await resp.Content.ReadAsStringAsync();
        System.Console.WriteLine($"GET /api/roles => {(int)resp.StatusCode} {resp.StatusCode}\n{respBody}");
        resp.IsSuccessStatusCode.Should().BeTrue(respBody);
        var roles = await resp.Content.ReadFromJsonAsync<RoleDto[]>();
        roles.Should().NotBeNull();
        roles!.Length.Should().BeGreaterThan(0);
    }

    private record LoginResponse(string Token);
    private record UserDto(string Id, string UserName, string? Email, bool MustChangePassword);
    private record RoleDto(string Id, string Name);
}
