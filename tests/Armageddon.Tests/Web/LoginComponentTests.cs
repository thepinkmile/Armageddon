using Armageddon.Web.Components.Account.Pages;
using Armageddon.Web.Services;
using Armageddon.Web.Components.Account;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http;

namespace Armageddon.Tests.Web;

public class LoginComponentTests : BunitContext
{
    private readonly Mock<AuthService> _authMock = new(MockBehavior.Strict,
        new HttpClient(),
        new Mock<IApiAuthenticationStateProvider>().Object,
        new Mock<IJSRuntime>().Object);

    public LoginComponentTests()
    {
        // Provide a dummy AuthService; the component only needs LoginAsync to be present for the test
        Services.AddSingleton<AuthService>(_authMock.Object);
        Services.AddSingleton<IdentityRedirectManager>(new IdentityRedirectManager(new NavigationManagerStub()));
        // Ensure a concrete ApiAuthenticationStateProvider is available for DI when components request AuthenticationStateProvider
        Services.AddScoped<AuthenticationStateProvider, Armageddon.Web.Services.ApiAuthenticationStateProvider>();
        Services.AddSingleton<IApiAuthenticationStateProvider>(new Mock<IApiAuthenticationStateProvider>().Object);
    }

    [Fact]
    public void Login_DoesNotThrow_WhenIAuthenticationServiceMissing()
    {
        // Render the component without providing an HttpContext or authentication services.
        // The component should not throw during initialization because SignOutAsync is guarded.
        var cut = Render<Login>();

        Assert.NotNull(cut.Markup);
    }

    private sealed class NavigationManagerStub : Microsoft.AspNetCore.Components.NavigationManager
    {
        public NavigationManagerStub() => Initialize("http://localhost/", "http://localhost/");

        protected override void NavigateToCore(string uri, bool forceLoad) { }
    }
}
