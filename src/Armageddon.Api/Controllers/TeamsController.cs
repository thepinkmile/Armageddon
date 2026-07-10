using Armageddon.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeamsController(ITeamService teamService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await teamService.GetAllTeamsAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var team = await teamService.GetTeamByIdAsync(id);
        return team is null ? NotFound() : Ok(team);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Team name is required.");
        var team = await teamService.AddTeamAsync(name);
        return CreatedAtAction(nameof(GetById), new { id = team.Id }, team);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var removed = await teamService.RemoveTeamAsync(id);
        return removed ? NoContent() : NotFound();
    }
}
