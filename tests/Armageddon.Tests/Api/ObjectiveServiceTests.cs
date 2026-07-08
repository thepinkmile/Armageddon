using Armageddon.Api.Data;
using Armageddon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Armageddon.Tests.Api;

public class ObjectiveServiceTests
{
    private static ArmageddonDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ArmageddonDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ArmageddonDbContext(options);
    }

    [Fact]
    public async Task GetAllObjectivesAsync_ReturnsAllObjectives()
    {
        await using var context = CreateContext(nameof(GetAllObjectivesAsync_ReturnsAllObjectives));
        context.Objectives.AddRange(
            new Abstractions.Models.Objective { Name = "Kills" },
            new Abstractions.Models.Objective { Name = "Captures" });
        await context.SaveChangesAsync();

        var service = new ObjectiveService(context);
        var result = (await service.GetAllObjectivesAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, o => o.Name == "Kills");
        Assert.Contains(result, o => o.Name == "Captures");
    }

    [Fact]
    public async Task GetObjectiveByIdAsync_ReturnsObjective_WhenExists()
    {
        await using var context = CreateContext(nameof(GetObjectiveByIdAsync_ReturnsObjective_WhenExists));
        var obj = new Abstractions.Models.Objective { Name = "Kills" };
        context.Objectives.Add(obj);
        await context.SaveChangesAsync();

        var service = new ObjectiveService(context);
        var result = await service.GetObjectiveByIdAsync(obj.Id);

        Assert.NotNull(result);
        Assert.Equal("Kills", result!.Name);
    }

    [Fact]
    public async Task GetObjectiveByIdAsync_ReturnsNull_WhenNotExists()
    {
        await using var context = CreateContext(nameof(GetObjectiveByIdAsync_ReturnsNull_WhenNotExists));
        var service = new ObjectiveService(context);
        var result = await service.GetObjectiveByIdAsync(99);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddObjectiveAsync_AddsAndReturnsObjective()
    {
        await using var context = CreateContext(nameof(AddObjectiveAsync_AddsAndReturnsObjective));
        var service = new ObjectiveService(context);

        var result = await service.AddObjectiveAsync("Assists");

        Assert.NotNull(result);
        Assert.Equal("Assists", result.Name);
        Assert.True(result.Id > 0);
        Assert.Equal(1, await context.Objectives.CountAsync());
    }

    [Fact]
    public async Task RemoveObjectiveAsync_ReturnsFalse_WhenNotExists()
    {
        await using var context = CreateContext(nameof(RemoveObjectiveAsync_ReturnsFalse_WhenNotExists));
        var service = new ObjectiveService(context);
        var result = await service.RemoveObjectiveAsync(99);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveObjectiveAsync_ReturnsTrue_AndRemovesObjective()
    {
        await using var context = CreateContext(nameof(RemoveObjectiveAsync_ReturnsTrue_AndRemovesObjective));
        var obj = new Abstractions.Models.Objective { Name = "Flags" };
        context.Objectives.Add(obj);
        await context.SaveChangesAsync();

        var service = new ObjectiveService(context);
        var result = await service.RemoveObjectiveAsync(obj.Id);

        Assert.True(result);
        Assert.Equal(0, await context.Objectives.CountAsync());
    }
}
