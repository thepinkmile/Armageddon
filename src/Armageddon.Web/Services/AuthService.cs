using Armageddon.Abstractions.Models;
using Microsoft.JSInterop;

namespace Armageddon.Web.Services;

public class AuthService(HttpClient client, TokenProvider tokenProvider, IApiAuthenticationStateProvider authStateProvider, IJSRuntime jsRuntime, ILogger<AuthService> logger)
{
    private const string TokenKey = "armageddon_token";

    public async Task<bool> LoginAsync(string identifier, string password)
    {
        logger.LogInformation("AuthService.LoginAsync: posting to api/auth/login for '{Identifier}'", identifier);
        var response = await client.PostAsJsonAsync("api/auth/login", new { Identifier = identifier, Password = password });
        logger.LogInformation("AuthService.LoginAsync: response {Status}", response.StatusCode);

        if (!response.IsSuccessStatusCode) return false;

        var data = await response.Content.ReadFromJsonAsync<LoginResponse>();
        if (data?.Token is null) return false;

        await PersistTokenAsync(data.Token);

        // Check if the user must change their password immediately
        var user = await GetCurrentUserAsync();
        tokenProvider.MustChangePassword = user?.MustChangePassword ?? false;

        return true;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        var resp = await client.GetAsync("api/auth/me");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<User>();
    }

    public async Task InitializeAsync()
    {
        try
        {
            var token = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (string.IsNullOrEmpty(token)) return;

            // Validate the token is still accepted by the API before restoring auth state
            SetBearerToken(token);
            var resp = await client.GetAsync("api/auth/me");
            if (resp.IsSuccessStatusCode)
            {
                var user = await resp.Content.ReadFromJsonAsync<User>();
                tokenProvider.MustChangePassword = user?.MustChangePassword ?? false;
                authStateProvider.NotifyUserAuthentication(token);
                logger.LogInformation("AuthService.InitializeAsync: restored valid session from localStorage");
            }
            else
            {
                // Token expired or rejected — clear it
                logger.LogInformation("AuthService.InitializeAsync: stored token rejected by API, clearing");
                tokenProvider.Token = null;
                await LogoutAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AuthService.InitializeAsync: failed");
        }
    }

    public async Task LogoutAsync()
    {
        try { await jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey); } catch { }
        tokenProvider.Token = null;
        tokenProvider.MustChangePassword = false;
        authStateProvider.NotifyUserLogout();
        try { await client.PostAsync("api/auth/logout", null); } catch { }
    }

    public async Task<bool> ChangePasswordAsync(string current, string newPassword)
    {
        var response = await client.PostAsJsonAsync("api/auth/change-password",
            new { CurrentPassword = current, NewPassword = newPassword });
        if (!response.IsSuccessStatusCode) return false;
        tokenProvider.MustChangePassword = false;
        return true;
    }

    private async Task PersistTokenAsync(string token)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AuthService.PersistTokenAsync: could not write to localStorage");
        }
        SetBearerToken(token);
        authStateProvider.NotifyUserAuthentication(token);
    }

    private void SetBearerToken(string token)
        => tokenProvider.Token = token;

    private record LoginResponse(string Token);
}
