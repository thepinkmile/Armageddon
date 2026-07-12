using Armageddon.Api.Data;
using Armageddon.UnitTests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Armageddon.UnitTests.Data;

public class IdentitySeederTests : IDisposable
{
    private readonly ServiceProvider _sp;

    public IdentitySeederTests()
    {
        var ctx = DbContextFactory.Create();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services.AddIdentityCore<ApplicationUser>(opt =>
            {
                // Allow simple password "admin" used by the seeder
                opt.Password.RequireDigit = false;
                opt.Password.RequiredLength = 4;
                opt.Password.RequireNonAlphanumeric = false;
                opt.Password.RequireUppercase = false;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ArmageddonDbContext>();

        _sp = services.BuildServiceProvider();
    }

    public void Dispose() => _sp.Dispose();

    // ── SeedRolesAndAdminAsync ─────────────────────────────────────────────

    [Fact]
    public async Task SeedRolesAndAdminAsync_CreatesRolesAndAdminUser()
    {
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);

        var roleManager = _sp.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();

        (await roleManager.RoleExistsAsync("Admin")).Should().BeTrue();
        (await roleManager.RoleExistsAsync("Viewer")).Should().BeTrue();

        var admin = await userManager.FindByEmailAsync("admin@armageddon.local");
        admin.Should().NotBeNull();
        admin!.UserName.Should().Be("admin");
        admin.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task SeedRolesAndAdminAsync_AssignsAdminRole()
    {
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);

        var userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@armageddon.local");
        var roles = await userManager.GetRolesAsync(admin!);

        roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task SeedRolesAndAdminAsync_IsIdempotent_WhenCalledTwice()
    {
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp); // second call must not throw

        var userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        var allAdmins = userManager.Users.Where(u => u.Email == "admin@armageddon.local").ToList();

        allAdmins.Should().HaveCount(1, "seeder must not create duplicate admin users");
    }

    [Fact]
    public async Task SeedRolesAndAdminAsync_AdminCanAuthenticateWithDefaultPassword()
    {
        await IdentitySeeder.SeedRolesAndAdminAsync(_sp);

        var userManager = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@armageddon.local");
        var passwordValid = await userManager.CheckPasswordAsync(admin!, "admin");

        passwordValid.Should().BeTrue();
    }

    [Fact]
    public async Task SeedRolesAndAdminAsync_Throws_WhenCreateUserFails()
    {
        // Build a service provider with password rules that will reject "admin"
        var ctx = DbContextFactory.Create();
        var strictServices = new ServiceCollection();
        strictServices.AddLogging();
        strictServices.AddSingleton(ctx);
        strictServices.AddIdentityCore<ApplicationUser>(opt =>
            {
                // Strict rules — "admin" (all lowercase, no digit, no special char) will fail
                opt.Password.RequireDigit = true;
                opt.Password.RequiredLength = 12;
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequireUppercase = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ArmageddonDbContext>();

        using var strictSp = strictServices.BuildServiceProvider();

        var act = () => IdentitySeeder.SeedRolesAndAdminAsync(strictSp);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*Failed to create the admin user*");
    }
}
