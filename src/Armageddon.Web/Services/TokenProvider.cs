namespace Armageddon.Web.Services;

/// <summary>
/// Singleton in-memory store for the current JWT bearer token and related user state.
/// Shared across all HttpClient instances so every API call carries the same token.
/// </summary>
public class TokenProvider
{
    public string? Token { get; set; }
    public bool MustChangePassword { get; set; }
}
