using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute.ReturnsExtensions;

namespace Armageddon.UnitTests.Controllers;

/// <summary>
/// Tests for RolesController using a mocked RoleManager.
/// </summary>
public class RolesControllerTests
{
    private static RoleManager<IdentityRole> MockRoleManager()
    {
        var store = Substitute.For<IRoleStore<IdentityRole>>();
        var validators = Enumerable.Empty<IRoleValidator<IdentityRole>>();
        var keyNorm = Substitute.For<ILookupNormalizer>();
        keyNorm.NormalizeName(Arg.Any<string?>()).Returns(x => ((string?)x[0])?.ToUpperInvariant());
        var errors = new IdentityErrorDescriber();
        var logger = Substitute.For<ILogger<RoleManager<IdentityRole>>>();
        return Substitute.For<RoleManager<IdentityRole>>(store, validators, keyNorm, errors, logger);
    }

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetAll_Returns200WithEmptyList_WhenNoRoles()
    {
        var rm = MockRoleManager();
        rm.Roles.Returns(Enumerable.Empty<IdentityRole>().AsQueryable());
        var sut = new RolesController(rm);

        var result = sut.GetAll();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void GetAll_Returns200WithRoles()
    {
        var rm = MockRoleManager();
        var roles = new List<IdentityRole> { new("Admin") { Id = "1" } }.AsQueryable();
        rm.Roles.Returns(roles);
        var sut = new RolesController(rm);

        var result = sut.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<RolesController.RoleDto>>();
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns400_WhenNameEmpty()
    {
        var sut = new RolesController(MockRoleManager());

        var result = await sut.Create(new RolesController.CreateRoleRequest(""));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_Returns201_WhenRoleCreated()
    {
        var rm = MockRoleManager();
        rm.CreateAsync(Arg.Any<IdentityRole>()).Returns(IdentityResult.Success);
        var sut = new RolesController(rm);

        var result = await sut.Create(new RolesController.CreateRoleRequest("Admin"));

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_Returns400_WhenCreateFails()
    {
        var rm = MockRoleManager();
        rm.CreateAsync(Arg.Any<IdentityRole>())
            .Returns(IdentityResult.Failed(new IdentityError { Description = "duplicate" }));
        var sut = new RolesController(rm);

        var result = await sut.Create(new RolesController.CreateRoleRequest("Admin"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        var rm = MockRoleManager();
        rm.FindByIdAsync("no-id").Returns(default(IdentityRole));
        var sut = new RolesController(rm);

        var result = await sut.Delete("no-id");

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Returns204_WhenDeleted()
    {
        var rm = MockRoleManager();
        var role = new IdentityRole("Admin") { Id = "r1" };
        rm.FindByIdAsync("r1").Returns(role);
        rm.DeleteAsync(role).Returns(IdentityResult.Success);
        var sut = new RolesController(rm);

        var result = await sut.Delete("r1");

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns400_WhenDeleteFails()
    {
        var rm = MockRoleManager();
        var role = new IdentityRole("Admin") { Id = "r2" };
        rm.FindByIdAsync("r2").Returns(role);
        rm.DeleteAsync(role).Returns(IdentityResult.Failed(new IdentityError { Description = "error" }));
        var sut = new RolesController(rm);

        var result = await sut.Delete("r2");

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
