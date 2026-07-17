using Armageddon.Api.Controllers;
using Armageddon.Api.Data;
using Armageddon.UnitTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Armageddon.UnitTests.Controllers;

/// <summary>
/// Uses a real EF in-memory Identity UserManager backed by ArmageddonDbContext.
/// ApplicationUser uses Guid as its key type.
/// </summary>
public class UsersControllerTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        var ctx = DbContextFactory.Create();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services.AddIdentityCore<ApplicationUser>(opt =>
        {
            // Relax password rules for test simplicity
            opt.Password.RequireDigit = false;
            opt.Password.RequiredLength = 4;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
        })
        .AddRoles<ApplicationRole>()
        .AddEntityFrameworkStores<ArmageddonDbContext>();

        _sp = services.BuildServiceProvider();
        _userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        _sut = new UsersController(_userManager);
    }

    public void Dispose() => _sp.Dispose();

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithEmptyList_WhenNoUsers()
    {
        var result = await _sut.GetAll();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAll_Returns200WithUsers_WhenUsersExist()
    {
        await _userManager.CreateAsync(new ApplicationUser { UserName = "alice", Email = "alice@test.com" }, "pass1");

        var result = await _sut.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    // ── Get ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Get_Returns404_WhenNotFound()
    {
        var result = await _sut.Get(Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Get_Returns200_WhenFound()
    {
        var user = new ApplicationUser { UserName = "bob", Email = "bob@test.com" };
        await _userManager.CreateAsync(user, "pass2");

        var result = await _sut.Get(user.Id.ToString());

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── Create ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns201_WhenValid()
    {
        var req = new UsersController.CreateUserRequest("carol", "carol@test.com", "pass3", false);

        var result = await _sut.Create(req);

        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Create_Returns400_WhenDuplicateUserName()
    {
        await _userManager.CreateAsync(new ApplicationUser { UserName = "dupe", Email = "dupe@test.com" }, "pass4");
        var req = new UsersController.CreateUserRequest("dupe", "dupe2@test.com", "pass5", false);

        var result = await _sut.Create(req);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Returns404_WhenNotFound()
    {
        var result = await _sut.Delete(Guid.NewGuid().ToString());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_Returns204_WhenDeleted()
    {
        var user = new ApplicationUser { UserName = "dave", Email = "dave@test.com" };
        await _userManager.CreateAsync(user, "pass6");

        var result = await _sut.Delete(user.Id.ToString());

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Returns400_WhenDeleteFails()
    {
        // Create a user with a mocked UserManager that returns failure on Delete
        var mockUm = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        var user = new ApplicationUser { UserName = "target" };
        mockUm.FindByIdAsync("uid").Returns(user);
        mockUm.DeleteAsync(user).Returns(
            IdentityResult.Failed(new IdentityError { Description = "Cannot delete" }));

        var controller = new UsersController(mockUm);

        var result = await controller.Delete("uid");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── UserDto record ─────────────────────────────────────────────────────

    [Fact]
    public void UserDto_CanBeConstructed()
    {
        var dto = new UsersController.UserDto("id-1", "alice", "alice@test.com", false);

        dto.Id.Should().Be("id-1");
        dto.UserName.Should().Be("alice");
        dto.Email.Should().Be("alice@test.com");
        dto.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public void UserDto_Equality_ByValue()
    {
        var d1 = new UsersController.UserDto("id-1", "alice", "alice@test.com", false);
        var d2 = new UsersController.UserDto("id-1", "alice", "alice@test.com", false);

        d1.Should().Be(d2);
    }
}
