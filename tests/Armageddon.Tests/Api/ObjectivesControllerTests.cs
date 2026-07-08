using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Armageddon.Tests.Api;

public class ObjectivesControllerTests
{
    private readonly Mock<IObjectiveService> _mockService = new();
    private ObjectivesController CreateController() => new(_mockService.Object);

    [Fact]
    public async Task GetAll_ReturnsOkWithObjectives()
    {
        var objectives = new List<Objective> { new() { Id = 1, Name = "Kills" } };
        _mockService.Setup(s => s.GetAllObjectivesAsync()).ReturnsAsync(objectives);

        var result = await CreateController().GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(objectives, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenFound()
    {
        var objective = new Objective { Id = 1, Name = "Kills" };
        _mockService.Setup(s => s.GetObjectiveByIdAsync(1)).ReturnsAsync(objective);

        var result = await CreateController().GetById(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(objective, ok.Value);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.GetObjectiveByIdAsync(99)).ReturnsAsync((Objective?)null);

        var result = await CreateController().GetById(99);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Add_ReturnsCreated_WithObjective()
    {
        var objective = new Objective { Id = 1, Name = "Assists" };
        _mockService.Setup(s => s.AddObjectiveAsync("Assists")).ReturnsAsync(objective);

        var result = await CreateController().Add("Assists");

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(objective, created.Value);
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
        _mockService.Setup(s => s.RemoveObjectiveAsync(1)).ReturnsAsync(true);

        var result = await CreateController().Remove(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Remove_ReturnsNotFound_WhenMissing()
    {
        _mockService.Setup(s => s.RemoveObjectiveAsync(99)).ReturnsAsync(false);

        var result = await CreateController().Remove(99);

        Assert.IsType<NotFoundResult>(result);
    }
}
