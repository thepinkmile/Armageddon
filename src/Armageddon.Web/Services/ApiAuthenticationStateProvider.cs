using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Armageddon.Web.Services;

public class ApiAuthenticationStateProvider : AuthenticationStateProvider, IApiAuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(_currentUser));

    public void NotifyUserAuthentication(string token)
    {
        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "apiauth");
        _currentUser = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    public void NotifyUserLogout()
    {
        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3) return [];

        // Pad base64url to standard base64
        var payload = parts[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
        var doc = JsonDocument.Parse(json);

        return doc.RootElement.EnumerateObject()
            .SelectMany(prop =>
            {
                if (prop.Value.ValueKind == JsonValueKind.Array)
                    return prop.Value.EnumerateArray()
                        .Select(v => new Claim(prop.Name, v.ToString()));
                return [new Claim(prop.Name, prop.Value.ToString())];
            });
    }
}
