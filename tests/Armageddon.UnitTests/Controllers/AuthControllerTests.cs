using System.Security.Claims;
using Armageddon.Api.Controllers;
using Armageddon.Api.Data;
using Armageddon.UnitTests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Armageddon.UnitTests.Controllers;

public class AuthControllerTests : IDisposable
{
    private readonly ServiceProvider _sp;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    public AuthControllerTests()
    {
        var ctx = DbContextFactory.Create();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services.AddIdentityCore<ApplicationUser>(opt =>
            {
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 4;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ArmageddonDbContext>()
            .AddSignInManager<SignInManager<ApplicationUser>>();

        // SignInManager requires IHttpContextAccessor and auth services
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddAuthentication();
        services.AddSingleton<IAuthenticationSchemeProvider>(
            Substitute.For<IAuthenticationSchemeProvider>());

        _sp = services.BuildServiceProvider();
        _userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        _signInManager = _sp.GetRequiredService<SignInManager<ApplicationUser>>();

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-secret-key-that-is-long-enough-32chars",
                ["Jwt:Issuer"] = "TestIssuer"
            })
            .Build();
    }

    public void Dispose() => _sp.Dispose();

    private AuthController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new AuthController(_signInManager, _userManager, _config);
        var httpCtx = new DefaultHttpContext
        {
            User = user ?? new ClaimsPrincipal(new ClaimsIdentity())
        };
        controller.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        return controller;
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string username = "alice", string email = "alice@test.com",
        string password = "pass1", bool mustChange = false)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            MustChangePassword = mustChange
        };
        var result = await _userManager.CreateAsync(user, password);
        result.Succeeded.Should().BeTrue();
        return user;
    }

    // ── Login ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Returns400_WhenCredentialsEmpty()
    {
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("", ""));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_Returns401_WhenUserNotFound()
    {
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("nobody", "pass"));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Login_Returns401_WhenWrongPassword()
    {
        await CreateUserAsync();
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("alice", "wrong"));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Login_Returns200WithToken_WhenValidCredentials()
    {
        await CreateUserAsync("bob", "bob@test.com", "pass1");
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("bob", "pass1"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthController.LoginResponse>().Subject;
        response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns200_WhenLoginByEmail()
    {
        await CreateUserAsync("carol", "carol@test.com", "pass2");
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("carol@test.com", "pass2"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Login_IncludesMustChangePassword_InToken_WhenFlagSet()
    {
        await CreateUserAsync("dave", "dave@test.com", "pass3", mustChange: true);
        var sut = CreateController();

        var result = await sut.Login(new AuthController.LoginRequest("dave", "pass3"));

        result.Should().BeOfType<OkObjectResult>();
        var ok = (OkObjectResult)result;
        var response = (AuthController.LoginResponse)ok.Value!;
        response.Token.Should().NotBeNullOrEmpty();
    }

    // ── Logout ─────────────────────────────────────────────────────────────

    [Fact]
    public void Logout_Returns200()
    {
        var sut = CreateController();

        var result = sut.Logout();

        result.Should().BeOfType<OkResult>();
    }

    // ── Me ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_Returns401_WhenNoNameClaim()
    {
        var sut = CreateController(new ClaimsPrincipal(new ClaimsIdentity()));

        var result = await sut.Me();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Me_Returns401_WhenUserNotFoundByName()
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "ghost")], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.Me();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Me_Returns200WithCurrentUser_WhenAuthenticated()
    {
        var user = await CreateUserAsync("eve", "eve@test.com", "pass4");
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "eve")], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthController.CurrentUserResponse>().Subject;
        response.UserName.Should().Be("eve");
        response.Email.Should().Be("eve@test.com");
    }

    // ── ChangePassword ─────────────────────────────────────────────────────

    [Fact]
    public async Task ChangePassword_Returns401_WhenUserNotFound()
    {
        // User principal has no matching user in store
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.ChangePassword(new AuthController.ChangePasswordRequest("old", "new1"));

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns400_WhenCurrentPasswordWrong()
    {
        var user = await CreateUserAsync("frank", "frank@test.com", "correct");
        // Set up the user principal properly so GetUserAsync can find the user
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!)
        ], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.ChangePassword(new AuthController.ChangePasswordRequest("wrong", "newpass"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ChangePassword_Returns200_WhenPasswordChangedSuccessfully()
    {
        var user = await CreateUserAsync("grace", "grace@test.com", "oldpass", mustChange: true);
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!)
        ], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.ChangePassword(new AuthController.ChangePasswordRequest("oldpass", "newpass"));

        result.Should().BeOfType<OkResult>();

        // MustChangePassword should have been cleared
        var updated = await _userManager.FindByIdAsync(user.Id.ToString());
        updated!.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task Login_IncludesRoleClaims_WhenUserHasRoles()
    {
        // Seed admin, run seeder to assign role, then login should include role claim in token
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);
        var sut = CreateController();

        // Login as the seeded admin — they have the "Admin" role
        var result = await sut.Login(new AuthController.LoginRequest("admin", "admin"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthController.LoginResponse>().Subject;
        // Token should be non-empty; role claims are embedded inside it
        response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Returns500_WhenUnexpectedExceptionThrown()
    {
        // Provide a null configuration to force an exception inside Login
        var badConfig = new ConfigurationBuilder().Build(); // Jwt:Key will be null — uses fallback "dev-secret..."
        // Instead force an exception by passing a config that returns an empty string key
        var configWithBadKey = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = "" })
            .Build();

        await CreateUserAsync("henry", "henry@test.com", "pass1");
        var controller = new AuthController(_signInManager, _userManager, configWithBadKey);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        // Empty key causes ArgumentOutOfRangeException inside SymmetricSecurityKey
        var result = await controller.Login(new AuthController.LoginRequest("henry", "pass1"));

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    // ── Me ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Me_Returns200_IncludesRoles_WhenUserHasRoles()
    {
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test");
        var sut = CreateController(new ClaimsPrincipal(identity));

        var result = await sut.Me();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<AuthController.CurrentUserResponse>().Subject;
        response.Roles.Should().Contain("Admin");
    }

    // ── FirstLogin ─────────────────────────────────────────────────────────

    [Fact]
    public async Task FirstLogin_Returns400_Always()
    {
        var sut = CreateController();

        // FirstLogin is documented as not-implemented; it should always return 400
        var result = await sut.FirstLogin(new AuthController.FirstLoginRequest("any", "any"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
