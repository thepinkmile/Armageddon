using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.UnitTests.Helpers;

/// <summary>
/// Creates a fresh in-memory <see cref="ArmageddonDbContext"/> for each test.
/// Each call uses a unique database name so tests are fully isolated.
/// </summary>
public static class DbContextFactory
{
    public static ArmageddonDbContext Create(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ArmageddonDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new ArmageddonDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
