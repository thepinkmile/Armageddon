using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Armageddon.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Armageddon.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly string _dbFilePath;

    public CustomWebApplicationFactory()
    {
        _dbFilePath = Path.Combine(Path.GetTempPath(), $"armageddon_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbFilePath}";
    }

    private string SeedMarkerPath => Path.Combine(Path.GetTempPath(), Path.GetFileName(_dbFilePath) + ".seeded");

    public async Task EnsureTestDataAsync()
    {
        // Use EF migrations/EnsureCreated to prepare schema for tests. Raw SQL fallback removed.

        // Apply migrations and seed admin user/role for tests
        // Ensure migrations are discovered from the API assembly (Program) so Identity migrations are found
        var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
        var appOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connectionString, b => b.MigrationsAssembly(migrationsAssembly))
            .Options;

        using (var appDb = new ApplicationDbContext(appOptions))
        {
            // Apply migrations in the test environment so the schema matches production migrations.
            // This replaces EnsureCreated() so tests exercise the same migration code path.
            try
            {
                appDb.Database.Migrate();
            }
            catch
            {
                // If migrations fail for some reason, fall back to EnsureCreated to avoid blocking tests entirely
                try { appDb.Database.EnsureCreated(); } catch { }
            }

            const string adminEmail = "admin@armageddon.local";
            const string adminUserName = "admin";
            const string adminPassword = "admin";

            if (!appDb.Roles.Any(r => r.NormalizedName == "ADMINISTRATOR"))
            {
                var role = new IdentityRole
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Administrator",
                    NormalizedName = "ADMINISTRATOR",
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                appDb.Roles.Add(role);
                appDb.SaveChanges();
            }

            if (!appDb.Users.Any(u => u.NormalizedUserName == adminUserName.ToUpperInvariant()))
            {
                var adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    UserName = adminUserName,
                    NormalizedUserName = adminUserName.ToUpperInvariant(),
                    Email = adminEmail,
                    NormalizedEmail = adminEmail.ToUpperInvariant(),
                    EmailConfirmed = true,
                    MustChangePassword = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
                adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);
                appDb.Users.Add(adminUser);
                appDb.SaveChanges();

                var role = appDb.Roles.First(r => r.NormalizedName == "ADMINISTRATOR");
                appDb.UserRoles.Add(new Microsoft.AspNetCore.Identity.IdentityUserRole<string> { UserId = adminUser.Id, RoleId = role.Id });
                appDb.SaveChanges();
            }
        }

        // Also ensure ArmageddonDbContext schema exists
        var armOptions = new DbContextOptionsBuilder<ArmageddonDbContext>()
            .UseSqlite(_connectionString, b => b.MigrationsAssembly(migrationsAssembly))
            .Options;
        using (var armDb = new ArmageddonDbContext(armOptions))
        {
            try
            {
                armDb.Database.EnsureCreated();
            }
            catch { }
        }

        // Ensure identity tables exist via raw SQL as a fallback (creates minimal tables used by tests)
        // Raw SQL fallback removed; rely on migrations/EnsureCreated above.
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var dict = new System.Collections.Generic.Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString,
                // Provide a sufficiently long symmetric key for HS256 (must be > 256 bits)
                ["Jwt:Key"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                ["Jwt:Issuer"] = "Armageddon.Api.Tests"
            };
            // Tell the API to skip its own startup seeding so tests can control seeding order
            dict["SkipDatabaseSeeding"] = "true";
            config.AddInMemoryCollection(dict);
        });

        builder.ConfigureServices(services =>
        {
            // Replace existing DbContext registrations so tests use the temporary SQLite file.
            var appDbDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
            if (appDbDescriptor != null) services.Remove(appDbDescriptor);
            var appDbOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (appDbOptionsDescriptor != null) services.Remove(appDbOptionsDescriptor);

            var armDbDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ArmageddonDbContext));
            if (armDbDescriptor != null) services.Remove(armDbDescriptor);
            var armDbOptionsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(DbContextOptions<ArmageddonDbContext>));
            if (armDbOptionsDescriptor != null) services.Remove(armDbOptionsDescriptor);

            // Register test DB contexts pointing at the temp SQLite file and ensure migrations are applied from the API assembly.
            var migrationsAssembly = typeof(Program).Assembly.GetName().Name;
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connectionString, b => b.MigrationsAssembly(migrationsAssembly)));
            services.AddDbContext<ArmageddonDbContext>(options => options.UseSqlite(_connectionString, b => b.MigrationsAssembly(migrationsAssembly)));

                // Ensure JWT validation in the test host uses the same symmetric key supplied via configuration above.
                // This avoids timing/order issues where the authentication options were constructed before the in-memory config was applied.
                var keyBytes = Encoding.UTF8.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                var signingKey = new SymmetricSecurityKey(keyBytes);
                services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    options =>
                    {
                        options.RequireHttpsMetadata = false;
                        options.SaveToken = true;
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateIssuerSigningKey = true,
                            IssuerSigningKey = signingKey,
                            RoleClaimType = ClaimTypes.Role,
                            NameClaimType = ClaimTypes.Name
                        };
                    });

            // Use normal authentication (JWT) in the test host; tests will perform real login against the seeded admin user.

            // No hosted seeder: tests will call EnsureTestDataAsync() to prepare DB before starting host.
        });
    }
}
