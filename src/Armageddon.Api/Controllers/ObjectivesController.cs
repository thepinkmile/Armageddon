using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
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
    public async Task<IActionResult> Add([FromBody] AddObjectiveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Objective name is required.");
        var objective = await objectiveService.AddObjectiveAsync(request.Name, request.Points, request.MaxUsage);
        return CreatedAtAction(nameof(GetById), new { id = objective.Id }, objective);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateObjectiveRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Objective name is required.");
        var objective = await objectiveService.UpdateObjectiveAsync(id, request.Name, request.Points, request.MaxUsage);
        return objective is null ? NotFound() : Ok(objective);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var removed = await objectiveService.RemoveObjectiveAsync(id);
        return removed ? NoContent() : NotFound();
    }
}

public record AddObjectiveRequest(string Name, int Points = 100, int? MaxUsage = null);
public record UpdateObjectiveRequest(string Name, int Points = 100, int? MaxUsage = null);
