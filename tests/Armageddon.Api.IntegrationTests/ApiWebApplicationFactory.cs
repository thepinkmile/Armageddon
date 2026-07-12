using Armageddon.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Armageddon.Api.IntegrationTests;

/// <summary>
/// Boots the real Armageddon API in-process with an isolated in-memory EF Core database.
/// Each factory instance gets its own unique database name so test collections cannot share state.
///
/// The factory explicitly seeds roles and the admin user after startup because Program.cs
/// calls IdentitySeeder only inside the Migrate() try-block, which throws on in-memory
/// databases and falls into a catch that only calls EnsureCreated — skipping the seeder.
/// </summary>
public class ApiWebApplicationFactory : WebApplicationFactory<Armageddon.Api.Program>
{
    private readonly string _dbName = $"ApiIntegration_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("UseInMemoryDatabase", "true");

        builder.ConfigureServices(services =>
        {
            // Swap out whatever DbContext registration Program.cs added and replace
            // with a fresh in-memory instance scoped to this factory run.
            services.RemoveAll<DbContextOptions<ArmageddonDbContext>>();
            services.RemoveAll<ArmageddonDbContext>();

            services.AddDbContext<ArmageddonDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>
    /// Called the first time a client is created.  After the host has started we
    /// ensure the schema exists and run the identity seeder so tests can log in as admin.
    /// </summary>
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        EnsureSeededAsync().GetAwaiter().GetResult();
    }

    private bool _seeded;

    private async Task EnsureSeededAsync()
    {
        if (_seeded) return;
        _seeded = true;

        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ArmageddonDbContext>();
        await db.Database.EnsureCreatedAsync();
        await IdentitySeeder.SeedRolesAndAdminAsync(sp);
    }
}
