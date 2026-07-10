using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Tests.Api;

public class TeamsControllerTests
{
    private readonly Mock<ITeamService> _mockService = new();
    private TeamsController CreateController() => new(_mockService.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithTeams()
    {
        var teams = new List<Team> { new() { Id = 1, Name = "Alpha" } };
        _mockService.Setup(s => s.GetAllTeamsAsync()).ReturnsAsync(teams);

        var result = await CreateController().GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(teams, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var team = new Team { Id = 1, Name = "Alpha" };
        _mockService.Setup(s => s.GetTeamByIdAsync(1)).ReturnsAsync(team);

        var result = await CreateController().GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(team, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.GetTeamByIdAsync(99)).ReturnsAsync((Team?)null);

        var result = await CreateController().GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsCreated_WithTeam()
    {
        var team = new Team { Id = 1, Name = "Bravo" };
        _mockService.Setup(s => s.AddTeamAsync("Bravo")).ReturnsAsync(team);

        var result = await CreateController().Add("Bravo");

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(team, created.Value);
    }

    [Fact]
    public async Task Add_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var result = await CreateController().Add("");
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Remove_ReturnsNoContent_WhenSuccessful()
    {
        _mockService.Setup(s => s.RemoveTeamAsync(1)).ReturnsAsync(true);

        var result = await CreateController().Remove(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.RemoveTeamAsync(99)).ReturnsAsync(false);

        var result = await CreateController().Remove(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
