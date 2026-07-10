using System.Net.Http.Headers;

namespace Armageddon.Web.Services;

/// <summary>
/// Automatically attaches the bearer token from TokenProvider to every outgoing HTTP request.
/// </summary>
public class AuthTokenHandler(TokenProvider tokenProvider) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(tokenProvider.Token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
