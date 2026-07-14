namespace Armageddon.Web.UnitTests;

public class AuthTokenHandlerTests
{
    private static HttpClient BuildClient(TokenProvider tokenProvider, MockHttpMessageHandler mockHttp)
    {
        var handler = new AuthTokenHandler(tokenProvider)
        {
            InnerHandler = mockHttp
        };
        return new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
    }

    [Fact]
    public async Task SendAsync_WithToken_AttachesBearerHeader()
    {
        var tokenProvider = new TokenProvider { Token = "my-jwt-token" };
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api").Respond(HttpStatusCode.OK);
        var client = BuildClient(tokenProvider, mockHttp);

        var response = await client.GetAsync("api");

        var request = mockHttp.GetMatchCount(mockHttp.When("http://test/api")) >= 0
            ? response.RequestMessage
            : null;
        response.RequestMessage!.Headers.Authorization.Should().NotBeNull();
        response.RequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        response.RequestMessage.Headers.Authorization.Parameter.Should().Be("my-jwt-token");
    }

    [Fact]
    public async Task SendAsync_WithoutToken_DoesNotAttachAuthorizationHeader()
    {
        var tokenProvider = new TokenProvider { Token = null };
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api").Respond(HttpStatusCode.OK);
        var client = BuildClient(tokenProvider, mockHttp);

        var response = await client.GetAsync("api");

        response.RequestMessage!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_WithEmptyToken_DoesNotAttachAuthorizationHeader()
    {
        var tokenProvider = new TokenProvider { Token = "" };
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api").Respond(HttpStatusCode.OK);
        var client = BuildClient(tokenProvider, mockHttp);

        var response = await client.GetAsync("api");

        response.RequestMessage!.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_TokenChangedBetweenRequests_UsesCurrentToken()
    {
        var tokenProvider = new TokenProvider { Token = "first-token" };
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("http://test/api").Respond(HttpStatusCode.OK);
        var client = BuildClient(tokenProvider, mockHttp);

        await client.GetAsync("api");

        tokenProvider.Token = "second-token";
        var response2 = await client.GetAsync("api");

        response2.RequestMessage!.Headers.Authorization!.Parameter.Should().Be("second-token");
    }
}
