using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.UnitTests.Controllers;

public class ObjectivesControllerTests
{
    private readonly IObjectiveService _service = Substitute.For<IObjectiveService>();
    private readonly ObjectivesController _sut;

    public ObjectivesControllerTests() => _sut = new ObjectivesController(_service);

    // ── GetAll ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithObjectives()
    {
        var objectives = new List<Objective> { new() { Id = 1, Name = "Kill", Points = 100 } };
        _service.GetAllObjectivesAsync().Returns(objectives);

        var result = await _sut.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(objectives);
    }

    // ── GetById ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_Returns404_WhenNotFound()
    {
        _service.GetObjectiveByIdAsync(99).Returns((Objective?)null);

        var result = await _sut.GetById(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetById_Returns200_WhenFound()
    {
        var obj = new Objective { Id = 2, Name = "Capture", Points = 200 };
        _service.GetObjectiveByIdAsync(2).Returns(obj);

        var result = await _sut.GetById(2);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(obj);
    }

    // ── Add ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_Returns400_WhenNameEmpty()
    {
        var result = await _sut.Add(new AddObjectiveRequest(""));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Add_Returns201_WhenValid()
    {
        var obj = new Objective { Id = 3, Name = "Scout", Points = 50 };
        _service.AddObjectiveAsync("Scout", 50, null).Returns(obj);

        var result = await _sut.Add(new AddObjectiveRequest("Scout", 50, null));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.Value.Should().Be(obj);
    }

    // ── Update ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_Returns400_WhenNameEmpty()
    {
        var result = await _sut.Update(1, new UpdateObjectiveRequest(""));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Update_Returns404_WhenNotFound()
    {
        _service.UpdateObjectiveAsync(99, "X", 10, null).Returns((Objective?)null);

        var result = await _sut.Update(99, new UpdateObjectiveRequest("X", 10, null));

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Update_Returns200_WhenUpdated()
    {
        var obj = new Objective { Id = 1, Name = "New", Points = 999 };
        _service.UpdateObjectiveAsync(1, "New", 999, null).Returns(obj);

        var result = await _sut.Update(1, new UpdateObjectiveRequest("New", 999, null));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(obj);
    }

    // ── Remove ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Remove_Returns404_WhenNotFound()
    {
        _service.RemoveObjectiveAsync(99).Returns(false);

        var result = await _sut.Remove(99);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Remove_Returns204_WhenRemoved()
    {
        _service.RemoveObjectiveAsync(1).Returns(true);

        var result = await _sut.Remove(1);

        result.Should().BeOfType<NoContentResult>();
    }
}
