using Armageddon.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ObjectivesController(IObjectiveService objectiveService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await objectiveService.GetAllObjectivesAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var objective = await objectiveService.GetObjectiveByIdAsync(id);
        return objective is null ? NotFound() : Ok(objective);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Objective name is required.");
        var objective = await objectiveService.AddObjectiveAsync(name);
        return CreatedAtAction(nameof(GetById), new { id = objective.Id }, objective);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var removed = await objectiveService.RemoveObjectiveAsync(id);
        return removed ? NoContent() : NotFound();
    }
}
