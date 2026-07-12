using Armageddon.Abstractions.Models;
using Armageddon.Api.Services;
using Armageddon.UnitTests.Helpers;
using FluentAssertions;

namespace Armageddon.UnitTests.Services;

public class TeamServiceTests
{
    // ── GetAllTeamsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetAllTeamsAsync_ReturnsEmpty_WhenNoTeams()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TeamService(ctx);

        var result = await sut.GetAllTeamsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTeamsAsync_ReturnsAllTeams()
    {
        using var ctx = DbContextFactory.Create();
        ctx.Teams.AddRange(new Team { Name = "Alpha" }, new Team { Name = "Beta" });
        await ctx.SaveChangesAsync();
        var sut = new TeamService(ctx);

        var result = await sut.GetAllTeamsAsync();

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().Contain(["Alpha", "Beta"]);
    }

    // ── GetTeamByIdAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamByIdAsync_ReturnsNull_WhenNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TeamService(ctx);

        var result = await sut.GetTeamByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamByIdAsync_ReturnsTeam_WhenFound()
    {
        using var ctx = DbContextFactory.Create();
        var team = new Team { Name = "Gamma" };
        ctx.Teams.Add(team);
        await ctx.SaveChangesAsync();
        var sut = new TeamService(ctx);

        var result = await sut.GetTeamByIdAsync(team.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Gamma");
    }

    // ── AddTeamAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddTeamAsync_PersistsAndReturnsTeam()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TeamService(ctx);

        var result = await sut.AddTeamAsync("Delta");

        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("Delta");
        ctx.Teams.Should().ContainSingle(t => t.Name == "Delta");
    }

    // ── RemoveTeamAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveTeamAsync_ReturnsFalse_WhenTeamNotFound()
    {
        using var ctx = DbContextFactory.Create();
        var sut = new TeamService(ctx);

        var result = await sut.RemoveTeamAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveTeamAsync_ReturnsTrue_AndRemovesTeam()
    {
        using var ctx = DbContextFactory.Create();
        var team = new Team { Name = "Epsilon" };
        ctx.Teams.Add(team);
        await ctx.SaveChangesAsync();
        var sut = new TeamService(ctx);

        var result = await sut.RemoveTeamAsync(team.Id);

        result.Should().BeTrue();
        ctx.Teams.Should().BeEmpty();
    }
}
