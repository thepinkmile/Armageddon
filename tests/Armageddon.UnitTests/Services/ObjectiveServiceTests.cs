using Armageddon.Abstractions.Models;
using Armageddon.Api.Services;
using Armageddon.UnitTests.Helpers;
using FluentAssertions;

namespace Armageddon.UnitTests.Services;

public class ObjectiveServiceTests
{
    // ── GetAllObjectivesAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetAllObjectivesAsync_ReturnsEmpty_WhenNone()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.GetAllObjectivesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllObjectivesAsync_ReturnsAll()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Objectives.AddRange(
            new Objective { Name = "Kill", Points = 10 },
            new Objective { Name = "Capture", Points = 20 });
        await ctx.SaveChangesAsync();
        var sut = new ObjectiveService(ctx);

        var result = await sut.GetAllObjectivesAsync();

        result.Should().HaveCount(2);
    }

    // ── GetObjectiveByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetObjectiveByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.GetObjectiveByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetObjectiveByIdAsync_ReturnsObjective_WhenFound()
    {
        using var ctx = DbContextFactory.Create();
        var obj = new Objective { Name = "Defend", Points = 50, MaxUsage = 3 };
        ctx.Objectives.Add(obj);
        await ctx.SaveChangesAsync();
        var sut = new ObjectiveService(ctx);

        var result = await sut.GetObjectiveByIdAsync(obj.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Defend");
        result.Points.Should().Be(50);
        result.MaxUsage.Should().Be(3);
    }

    // ── AddObjectiveAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task AddObjectiveAsync_PersistsWithDefaultPoints()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.AddObjectiveAsync("Scout");

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Scout");
        result.Points.Should().Be(100);
        result.MaxUsage.Should().BeNull();
    }

    [Fact]
    public async Task AddObjectiveAsync_PersistsWithCustomValues()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.AddObjectiveAsync("Elite Kill", 250, 1);

        result.Points.Should().Be(250);
        result.MaxUsage.Should().Be(1);
    }

    // ── UpdateObjectiveAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateObjectiveAsync_ReturnsNull_WhenNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.UpdateObjectiveAsync(99, "X", 1, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateObjectiveAsync_UpdatesAllFields()
    {
        using var ctx = DbContextFactory.Create();
        var obj = new Objective { Name = "Old", Points = 10, MaxUsage = 2 };
        ctx.Objectives.Add(obj);
        await ctx.SaveChangesAsync();
        var sut = new ObjectiveService(ctx);

        var result = await sut.UpdateObjectiveAsync(obj.Id, "New", 999, 5);

        result.Should().NotBeNull();
        result!.Name.Should().Be("New");
        result.Points.Should().Be(999);
        result.MaxUsage.Should().Be(5);
    }

    [Fact]
    public async Task UpdateObjectiveAsync_CanClearMaxUsage()
    {
        using var ctx = DbContextFactory.Create();
        var obj = new Objective { Name = "Obj", Points = 10, MaxUsage = 3 };
        ctx.Objectives.Add(obj);
        await ctx.SaveChangesAsync();
        var sut = new ObjectiveService(ctx);

        var result = await sut.UpdateObjectiveAsync(obj.Id, "Obj", 10, null);

        result!.MaxUsage.Should().BeNull();
    }

    // ── RemoveObjectiveAsync ───────────────────────────────────────────────

    [Fact]
    public async Task RemoveObjectiveAsync_ReturnsFalse_WhenNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new ObjectiveService(ctx);

        var result = await sut.RemoveObjectiveAsync(99);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveObjectiveAsync_ReturnsTrue_AndRemoves()
    {
        using var ctx = DbContextFactory.Create();
        var obj = new Objective { Name = "Temp", Points = 10 };
        ctx.Objectives.Add(obj);
        await ctx.SaveChangesAsync();
        var sut = new ObjectiveService(ctx);

        var result = await sut.RemoveObjectiveAsync(obj.Id);

        result.Should().BeTrue();
        ctx.Objectives.Should().BeEmpty();
    }
}
