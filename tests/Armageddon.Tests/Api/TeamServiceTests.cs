using Armageddon.Api.Data;
using Armageddon.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Armageddon.Tests.Api;

public class TeamServiceTests
{
    private static ArmageddonDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ArmageddonDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ArmageddonDbContext(options);
    }

    [Fact]
    public async Task GetAllTeamsAsync_ReturnsAllTeams()
    {
        await using var context = CreateContext(nameof(GetAllTeamsAsync_ReturnsAllTeams));
        context.Teams.AddRange(
            new Abstractions.Models.Team { Name = "Alpha" },
            new Abstractions.Models.Team { Name = "Bravo" });
        await context.SaveChangesAsync();

        var service = new TeamService(context);
        var result = (await service.GetAllTeamsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Name == "Alpha");
        Assert.Contains(result, t => t.Name == "Bravo");
    }

    [Fact]
    public async Task GetTeamByIdAsync_ReturnsTeam_WhenExists()
    {
        await using var context = CreateContext(nameof(GetTeamByIdAsync_ReturnsTeam_WhenExists));
        var team = new Abstractions.Models.Team { Name = "Alpha" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var service = new TeamService(context);
        var result = await service.GetTeamByIdAsync(team.Id);

        Assert.NotNull(result);
        Assert.Equal("Alpha", result!.Name);
    }

    [Fact]
    public async Task GetTeamByIdAsync_ReturnsNull_WhenNotExists()
    {
        await using var context = CreateContext(nameof(GetTeamByIdAsync_ReturnsNull_WhenNotExists));
        var service = new TeamService(context);
        var result = await service.GetTeamByIdAsync(99);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddTeamAsync_AddsAndReturnsTeam()
    {
        await using var context = CreateContext(nameof(AddTeamAsync_AddsAndReturnsTeam));
        var service = new TeamService(context);

        var result = await service.AddTeamAsync("Delta");

        Assert.NotNull(result);
        Assert.Equal("Delta", result.Name);
        Assert.True(result.Id > 0);
        Assert.Equal(1, await context.Teams.CountAsync());
    }

    [Fact]
    public async Task RemoveTeamAsync_ReturnsFalse_WhenNotExists()
    {
        await using var context = CreateContext(nameof(RemoveTeamAsync_ReturnsFalse_WhenNotExists));
        var service = new TeamService(context);
        var result = await service.RemoveTeamAsync(99);
        Assert.False(result);
    }

    [Fact]
    public async Task RemoveTeamAsync_ReturnsTrue_AndRemovesTeam()
    {
        await using var context = CreateContext(nameof(RemoveTeamAsync_ReturnsTrue_AndRemovesTeam));
        var team = new Abstractions.Models.Team { Name = "Echo" };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        var service = new TeamService(context);
        var result = await service.RemoveTeamAsync(team.Id);

        Assert.True(result);
        Assert.Equal(0, await context.Teams.CountAsync());
    }
}
