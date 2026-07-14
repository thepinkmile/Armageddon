using Armageddon.Abstractions.Models;
using Microsoft.JSInterop;

namespace Armageddon.Web.UnitTests;

/// <summary>
/// Unit tests for <see cref="AuthService"/>.
/// HttpClient is backed by <see cref="MockHttpMessageHandler"/> so no network is needed.
/// IJSRuntime and IApiAuthenticationStateProvider are NSubstitute mocks.
/// </summary>
public class AuthServiceTests
{
    private const string TokenKey = "armageddon_token";

    private static readonly User SampleUser = new(
        Guid.NewGuid(), "alice", "alice@example.com",
        MustChangePassword: false, Enabled: true, Roles: ["User"]);

    private static readonly User MustChangeUser = new(
        Guid.NewGuid(), "bob", "bob@example.com",
        MustChangePassword: true, Enabled: true, Roles: ["User"]);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (AuthService sut, MockHttpMessageHandler mockHttp, TokenProvider tokenProvider,
        IApiAuthenticationStateProvider authState, IJSRuntime jsRuntime)
        CreateSut()
    {
        var mockHttp    = new MockHttpMessageHandler();
        var client      = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("http://api/");

        var tokenProvider = new TokenProvider();
        var authState     = Substitute.For<IApiAuthenticationStateProvider>();
        var jsRuntime     = Substitute.For<IJSRuntime>();
        var logger        = NullLogger<AuthService>.Instance;

        var sut = new AuthService(client, tokenProvider, authState, jsRuntime, logger);
        return (sut, mockHttp, tokenProvider, authState, jsRuntime);
    }

    // ── LoginAsync ────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_SuccessfulResponse_ReturnsTrue()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":"jwt-token"}""");
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        var result = await sut.LoginAsync("alice", "secret");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulResponse_SetsTokenOnProvider()
    {
        var (sut, mockHttp, tokenProvider, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":"my-jwt"}""");
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        await sut.LoginAsync("alice", "secret");

        tokenProvider.Token.Should().Be("my-jwt");
    }

    [Fact]
    public async Task LoginAsync_SuccessfulResponse_NotifiesAuthState()
    {
        var (sut, mockHttp, _, authState, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":"my-jwt"}""");
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        await sut.LoginAsync("alice", "secret");

        authState.Received(1).NotifyUserAuthentication("my-jwt");
    }

    [Fact]
    public async Task LoginAsync_SuccessfulResponse_SetsMustChangePassword_WhenRequired()
    {
        var (sut, mockHttp, tokenProvider, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":"my-jwt"}""");
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(MustChangeUser));

        await sut.LoginAsync("bob", "secret");

        tokenProvider.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_SuccessfulResponse_DoesNotSetMustChangePassword_WhenNotRequired()
    {
        var (sut, mockHttp, tokenProvider, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":"my-jwt"}""");
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        await sut.LoginAsync("alice", "secret");

        tokenProvider.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_FailedResponse_ReturnsFalse()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond(HttpStatusCode.Unauthorized);

        var result = await sut.LoginAsync("alice", "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_NullTokenInResponse_ReturnsFalse()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/login")
            .Respond("application/json", """{"token":null}""");

        var result = await sut.LoginAsync("alice", "secret");

        result.Should().BeFalse();
    }

    // ── GetCurrentUserAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUserAsync_SuccessfulResponse_ReturnsUser()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        var user = await sut.GetCurrentUserAsync();

        user.Should().NotBeNull();
        user!.UserName.Should().Be("alice");
    }

    [Fact]
    public async Task GetCurrentUserAsync_FailedResponse_ReturnsNull()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond(HttpStatusCode.Unauthorized);

        var user = await sut.GetCurrentUserAsync();

        user.Should().BeNull();
    }

    // ── LogoutAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_ClearsToken()
    {
        var (sut, mockHttp, tokenProvider, _, jsRuntime) = CreateSut();
        tokenProvider.Token = "existing-token";
        tokenProvider.MustChangePassword = true;
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/logout")
            .Respond(HttpStatusCode.OK);
        jsRuntime.InvokeAsync<object>(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));

        await sut.LogoutAsync();

        tokenProvider.Token.Should().BeNull();
        tokenProvider.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_NotifiesAuthStateLogout()
    {
        var (sut, mockHttp, _, authState, jsRuntime) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/logout")
            .Respond(HttpStatusCode.OK);
        jsRuntime.InvokeAsync<object>(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(new ValueTask<object>(new object()));

        await sut.LogoutAsync();

        authState.Received(1).NotifyUserLogout();
    }

    [Fact]
    public async Task LogoutAsync_JsRuntimeThrows_DoesNotPropagate()
    {
        var (sut, mockHttp, _, _, jsRuntime) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/logout")
            .Respond(HttpStatusCode.OK);
        jsRuntime.InvokeAsync<object>(Arg.Any<string>(), Arg.Any<object[]>())
            .Returns(x => throw new JSException("storage error"));

        var act = async () => await sut.LogoutAsync();

        await act.Should().NotThrowAsync();
    }

    // ── ChangePasswordAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ChangePasswordAsync_SuccessfulResponse_ReturnsTrue()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/change-password")
            .Respond(HttpStatusCode.OK);

        var result = await sut.ChangePasswordAsync("old", "new");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_Success_ClearsMustChangePassword()
    {
        var (sut, mockHttp, tokenProvider, _, _) = CreateSut();
        tokenProvider.MustChangePassword = true;
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/change-password")
            .Respond(HttpStatusCode.OK);

        await sut.ChangePasswordAsync("old", "new");

        tokenProvider.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_FailedResponse_ReturnsFalse()
    {
        var (sut, mockHttp, _, _, _) = CreateSut();
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/change-password")
            .Respond(HttpStatusCode.BadRequest);

        var result = await sut.ChangePasswordAsync("old", "wrong");

        result.Should().BeFalse();
    }

    // ── InitializeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task InitializeAsync_NoStoredToken_DoesNothing()
    {
        var (sut, _, tokenProvider, authState, jsRuntime) = CreateSut();
        jsRuntime.InvokeAsync<string?>(TokenKey, Arg.Any<object?[]?>())
            .Returns(new ValueTask<string?>(default(string)));

        await sut.InitializeAsync();

        tokenProvider.Token.Should().BeNull();
        authState.DidNotReceive().NotifyUserAuthentication(Arg.Any<string>());
    }

    [Fact]
    public async Task InitializeAsync_ValidStoredToken_RestoresSession()
    {
        var (sut, mockHttp, _, authState, jsRuntime) = CreateSut();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .Returns(new ValueTask<string?>("stored-jwt"));
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond("application/json", System.Text.Json.JsonSerializer.Serialize(SampleUser));

        await sut.InitializeAsync();

        authState.Received(1).NotifyUserAuthentication("stored-jwt");
    }

    [Fact]
    public async Task InitializeAsync_RejectedStoredToken_ClearsTokenAndLogsOut()
    {
        var (sut, mockHttp, tokenProvider, authState, jsRuntime) = CreateSut();
        jsRuntime.InvokeAsync<string?>("localStorage.getItem", Arg.Any<object?[]?>())
            .Returns(new ValueTask<string?>("expired-jwt"));
        mockHttp.When(HttpMethod.Get, "http://api/api/auth/me")
            .Respond(HttpStatusCode.Unauthorized);
        mockHttp.When(HttpMethod.Post, "http://api/api/auth/logout")
            .Respond(HttpStatusCode.OK);

        await sut.InitializeAsync();

        tokenProvider.Token.Should().BeNull();
        authState.Received(1).NotifyUserLogout();
    }

    [Fact]
    public async Task InitializeAsync_JsRuntimeThrows_DoesNotPropagate()
    {
        var (sut, _, _, _, jsRuntime) = CreateSut();
        jsRuntime.InvokeAsync<string?>(Arg.Any<string>(), Arg.Any<object?[]?>())
            .Returns(ValueTask.FromException<string?>(new JSException("storage unavailable")));

        var act = async () => await sut.InitializeAsync();

        await act.Should().NotThrowAsync();
    }
}
