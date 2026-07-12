using Armageddon.Abstractions.Models;
using Armageddon.Web.Services;
using Bunit;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Armageddon.Web.IntegrationTests;

/// <summary>
/// Base bunit 2.x context that wires up:
/// - A mocked <see cref="IArmageddonApiClient"/> (NSubstitute)
/// - A <see cref="BunitAuthorizationContext"/> for auth-state control
/// - All required Web DI services (TokenProvider, IApiAuthenticationStateProvider, AuthService)
/// </summary>
public class WebTestContext : BunitContext
{
    public IArmageddonApiClient ApiClient { get; }
    public BunitAuthorizationContext AuthCtx { get; }

    public WebTestContext()
    {
        // ── IArmageddonApiClient mock ────────────────────────────────────
        ApiClient = Substitute.For<IArmageddonApiClient>();
        ApiClient.GetTeamsAsync().Returns(Task.FromResult<IEnumerable<Team>>([]));
        ApiClient.GetObjectivesAsync().Returns(Task.FromResult<IEnumerable<Objective>>([]));
        ApiClient.GetRoundsAsync().Returns(Task.FromResult<IEnumerable<Round>>([]));
        ApiClient.GetScoresAsync().Returns(Task.FromResult<IEnumerable<Score>>([]));
        ApiClient.GetMatchesAsync().Returns(Task.FromResult<IEnumerable<Match>>([]));
        ApiClient.GetTournamentResultsAsync().Returns(Task.FromResult<IEnumerable<TournamentResult>>([]));
        ApiClient.GetCurrentUserAsync().Returns(Task.FromResult<User?>(null));
        ApiClient.GetUsersAsync().Returns(Task.FromResult<IEnumerable<User>>([]));
        ApiClient.GetRolesAsync().Returns(Task.FromResult<IEnumerable<Role>>([]));
        ApiClient.GetTournamentAsync().Returns(Task.FromResult(new Tournament()));

        // ── bunit authorization ──────────────────────────────────────────
        AuthCtx = this.AddAuthorization();
        AuthCtx.SetNotAuthorized();

        // ── Supporting services ──────────────────────────────────────────
        var tokenProvider = new TokenProvider();
        Services.AddSingleton(tokenProvider);
        Services.AddSingleton(ApiClient);

        var fakeAuthState = Substitute.For<IApiAuthenticationStateProvider>();
        Services.AddSingleton<IApiAuthenticationStateProvider>(fakeAuthState);

        // AuthService needs an HttpClient — provide a no-op one; individual tests
        // that exercise AuthService behaviour configure MockHttp per test.
        var httpClient = new HttpClient(new NoOpHttpHandler())
        {
            BaseAddress = new Uri("http://localhost")
        };
        Services.AddSingleton(sp =>
            new AuthService(
                httpClient,
                tokenProvider,
                fakeAuthState,
                JSInterop.JSRuntime,
                NullLogger<AuthService>.Instance));

        Services.AddLogging();
        Services.AddOptions();
    }

    public void SetAuthenticated(string username = "admin", params string[] roles)
    {
        AuthCtx.SetAuthorized(username);
        if (roles.Length > 0)
            AuthCtx.SetRoles(roles);
    }

    public void SetUnauthenticated() => AuthCtx.SetNotAuthorized();

    /// <summary>
    /// HTTP handler that can be overridden per-test by configuring <see cref="TestHttpHandler"/>.
    /// </summary>
    public TestHttpHandler NoOp { get; } = new TestHttpHandler();

    private sealed class NoOpHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}

/// <summary>Configurable per-test HTTP handler for AuthService calls.</summary>
public class TestHttpHandler : HttpMessageHandler
{
    public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = Handler?.Invoke(request)
            ?? new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        return Task.FromResult(response);
    }
}
