using System.Net.Http.Json;

namespace Armageddon.Api.IntegrationTests;

internal static class HttpClientExtensions
{
    private record LoginRequest(string Identifier, string Password);
    private record LoginResponse(string Token);

    /// <summary>Authenticates as the seeded admin user and attaches the JWT to the client.</summary>
    public static async Task AuthenticateAsAdminAsync(this HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("admin", "admin"));

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", body!.Token);
    }
}
