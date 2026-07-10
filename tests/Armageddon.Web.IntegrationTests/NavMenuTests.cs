using Armageddon.Web.Components.Layout;
using Bunit;
using FluentAssertions;

namespace Armageddon.Web.IntegrationTests;

/// <summary>
/// Tests for NavMenu.razor — verifies nav items are shown/hidden based on auth state and role.
/// </summary>
public class NavMenuTests : IDisposable
{
    private readonly WebTestContext _ctx;
    public NavMenuTests() => _ctx = new WebTestContext();
    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void NavMenu_AlwaysShowsTournamentLink()
    {
        _ctx.SetUnauthenticated();
        var cut = _ctx.Render<NavMenu>();
        cut.Find("a[href='knockout']").Should().NotBeNull();
    }

    [Fact]
    public void NavMenu_AlwaysShowsHistoryLink()
    {
        _ctx.SetUnauthenticated();
        var cut = _ctx.Render<NavMenu>();
        cut.Find("a[href='history']").Should().NotBeNull();
    }

    [Fact]
    public void NavMenu_HidesGameSetupLink_WhenUnauthenticated()
    {
        _ctx.SetUnauthenticated();
        var cut = _ctx.Render<NavMenu>();
        cut.FindAll("a[href='scoring']").Should().BeEmpty("Game Setup is only visible when authenticated");
    }

    [Fact]
    public void NavMenu_ShowsGameSetupLink_WhenAuthenticated()
    {
        _ctx.SetAuthenticated("admin", "Admin");
        var cut = _ctx.Render<NavMenu>();
        cut.Find("a[href='scoring']").Should().NotBeNull();
    }

    [Fact]
    public void NavMenu_HidesUserManagementLink_WhenNotAdministrator()
    {
        _ctx.SetAuthenticated("viewer", "Viewer");
        var cut = _ctx.Render<NavMenu>();
        cut.FindAll("a[href='admin/users']").Should().BeEmpty("User Management only visible to Administrator role");
    }

    [Fact]
    public void NavMenu_ShowsUserManagementLink_WhenAdministrator()
    {
        _ctx.SetAuthenticated("admin", "Administrator");
        var cut = _ctx.Render<NavMenu>();
        cut.Find("a[href='admin/users']").Should().NotBeNull();
    }

    [Fact]
    public void NavMenu_ShowsRoleManagementLink_WhenAdministrator()
    {
        _ctx.SetAuthenticated("admin", "Administrator");
        var cut = _ctx.Render<NavMenu>();
        cut.Find("a[href='admin/roles']").Should().NotBeNull();
    }
}
