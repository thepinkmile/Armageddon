using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;

namespace Armageddon.UnitTests.Data;

public class ApplicationUserRoleTests
{
    // ── ApplicationUser ────────────────────────────────────────────────────

    [Fact]
    public void ApplicationUser_DefaultProperties()
    {
        var user = new ApplicationUser();

        user.MustChangePassword.Should().BeTrue();
        user.Enabled.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_ToDto_MapsAllFields()
    {
        var id = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = id,
            UserName = "alice",
            Email = "alice@test.com",
            MustChangePassword = false,
            Enabled = true
        };

        var dto = user.ToDto();

        dto.Id.Should().Be(id);
        dto.UserName.Should().Be("alice");
        dto.Email.Should().Be("alice@test.com");
        dto.MustChangePassword.Should().BeFalse();
        dto.Enabled.Should().BeTrue();
        dto.Roles.Should().BeEmpty();
    }

    [Fact]
    public void ApplicationUser_FromDto_MapsAllFields()
    {
        var id = Guid.NewGuid();
        var dto = new User(id, "bob", "bob@test.com", true, false, ["Admin"]);

        var user = ApplicationUser.FromDto(dto);

        user.Id.Should().Be(id);
        user.UserName.Should().Be("bob");
        user.Email.Should().Be("bob@test.com");
        user.MustChangePassword.Should().BeTrue();
        user.Enabled.Should().BeFalse();
        user.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ApplicationUser_ToDto_WithNullableFields()
    {
        var user = new ApplicationUser
        {
            UserName = null,
            Email = null,
            MustChangePassword = true,
            Enabled = false
        };

        var dto = user.ToDto();

        dto.UserName.Should().BeNull();
        dto.Email.Should().BeNull();
        dto.MustChangePassword.Should().BeTrue();
        dto.Enabled.Should().BeFalse();
    }

    // ── ApplicationRole ────────────────────────────────────────────────────

    [Fact]
    public void ApplicationRole_ParameterlessConstructor_DefaultsEnabled()
    {
        var role = new ApplicationRole();

        role.Enabled.Should().BeTrue();
        role.Name.Should().BeNull();
    }

    [Fact]
    public void ApplicationRole_ParameterlessConstructor_WithEnabledFalse()
    {
        var role = new ApplicationRole(enabled: false);

        role.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ApplicationRole_NamedConstructor_SetsNameAndEnabled()
    {
        var role = new ApplicationRole("Moderator", enabled: true);

        role.Name.Should().Be("Moderator");
        role.Enabled.Should().BeTrue();
    }

    [Fact]
    public void ApplicationRole_NamedConstructor_DisabledRole()
    {
        var role = new ApplicationRole("Viewer", enabled: false);

        role.Name.Should().Be("Viewer");
        role.Enabled.Should().BeFalse();
    }

    [Fact]
    public void ApplicationRole_ToDto_MapsAllFields()
    {
        var id = Guid.NewGuid();
        var role = new ApplicationRole("Admin", true) { Id = id };

        var dto = role.ToDto();

        dto.Id.Should().Be(id);
        dto.Name.Should().Be("Admin");
        dto.Enabled.Should().BeTrue();
    }

    [Fact]
    public void ApplicationRole_FromDto_MapsAllFields()
    {
        var id = Guid.NewGuid();
        var dto = new Role(id, "Admin", true);

        var role = ApplicationRole.FromDto(dto);

        role.Id.Should().Be(id);
        role.Name.Should().Be("Admin");
        role.Enabled.Should().BeTrue();
    }

    [Fact]
    public void ApplicationRole_FromDto_DisabledRole()
    {
        var id = Guid.NewGuid();
        var dto = new Role(id, "Disabled", false);

        var role = ApplicationRole.FromDto(dto);

        role.Enabled.Should().BeFalse();
    }
}
