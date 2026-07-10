using Armageddon.Web.Components.Account.Pages;
using Bunit;
using FluentAssertions;
using NSubstitute;

namespace Armageddon.Web.IntegrationTests;

/// <summary>
/// Tests for Login.razor — AuthService is provided via DI with a no-op/mocked HttpClient.
/// No real network calls are made.
/// </summary>
public class LoginPageTests : IDisposable
{
    private readonly WebTestContext _ctx;
    public LoginPageTests() => _ctx = new WebTestContext();
    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Login_RendersUsernameAndPasswordInputs()
    {
        var cut = _ctx.Render<Login>();
        cut.Find("#Input\\.Identifier").Should().NotBeNull();
        cut.Find("#Input\\.Password").Should().NotBeNull();
    }

    [Fact]
    public void Login_RendersSubmitButton()
    {
        var cut = _ctx.Render<Login>();
        cut.Find("button[type=submit]").TextContent.Trim().Should().Be("Log in");
    }

    [Fact]
    public void Login_ShowsNoErrorMessage_Initially()
    {
        var cut = _ctx.Render<Login>();
        cut.FindAll(".alert-danger").Should().BeEmpty();
    }

    [Fact]
    public void Login_RendersPageHeading()
    {
        var cut = _ctx.Render<Login>();
        cut.Find("h1").TextContent.Should().Contain("Log in");
    }

    [Fact]
    public void Login_HasUsernameLabel()
    {
        var cut = _ctx.Render<Login>();
        cut.Find("label[for='Input.Identifier']").TextContent.Should().Contain("Username or email");
    }

    [Fact]
    public void Login_HasPasswordLabel()
    {
        var cut = _ctx.Render<Login>();
        cut.Find("label[for='Input.Password']").TextContent.Should().Contain("Password");
    }
}
