using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Armageddon.Api.Data;

namespace Armageddon.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Armageddon.Api.Program>
{
    public CustomWebApplicationFactory()
    {
        // Ensure the API reads the in-memory flag from environment variables early during startup
        Environment.SetEnvironmentVariable("UseInMemoryDatabase", "true");
        Environment.SetEnvironmentVariable("SkipDatabaseSeeding", "true");
    }
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure the environment is Development for the test host
        builder.UseEnvironment("Development");
        var host = base.CreateHost(builder);

        // Seed test data after the host is built so all services (including Identity stores) are available
        using (var scope = host.Services.CreateScope())
        {
            var scoped = scope.ServiceProvider;
            // Ensure the test SQLite in-memory databases are created
            var idDb = scoped.GetRequiredService<ApplicationDbContext>();
            var appDb = scoped.GetRequiredService<ArmageddonDbContext>();
            idDb.Database.EnsureCreated();
            appDb.Database.EnsureCreated();
            var userManager = scoped.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scoped.GetRequiredService<RoleManager<IdentityRole>>();

            if (!roleManager.RoleExistsAsync("Administrator").GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole("Administrator")).GetAwaiter().GetResult();
            }

            var admin = userManager.FindByNameAsync("admin").GetAwaiter().GetResult();
            if (admin is null)
            {
                admin = new ApplicationUser { UserName = "admin", Email = "admin@armageddon.local", EmailConfirmed = true, MustChangePassword = false };
                var create = userManager.CreateAsync(admin, "admin").GetAwaiter().GetResult();
                if (create.Succeeded)
                {
                    userManager.AddToRoleAsync(admin, "Administrator").GetAwaiter().GetResult();
                }
            }
        }

        return host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
            builder.ConfigureServices(services =>
            {
                // Remove only existing DbContextOptions registrations so we can re-register with a test provider
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
                services.RemoveAll(typeof(DbContextOptions<ArmageddonDbContext>));

                // Remove any existing DbContext registrations so our test registrations take effect
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<ArmageddonDbContext>();

                // Use the EF Core InMemory provider for tests (single provider for both contexts)
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("TestIdentityDb"));
                services.AddDbContext<ArmageddonDbContext>(options => options.UseInMemoryDatabase("TestAppDb"));
            });

            // Prevent the real Program from attempting to seed/migrate the production DB during tests.
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var dict = new System.Collections.Generic.Dictionary<string, string?>
                {
                    ["SkipDatabaseSeeding"] = "true",
                    ["UseInMemoryDatabase"] = "true"
                };
                // Ensure JWT signing key is large enough for HS256 during tests
                dict["Jwt:Key"] = "test-signing-key-that-is-long-enough-for-hs256-tests-0123456789";
                // Build a temporary IConfiguration and replace the existing configuration source
                var memoryConfig = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                    .AddInMemoryCollection(dict)
                    .Build();
                context.HostingEnvironment.ApplicationName = "Armageddon.Api";
                config.AddConfiguration(memoryConfig);
            });
    }
}
