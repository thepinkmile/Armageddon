using Armageddon.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScoresController(IScoreService scoreService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await scoreService.GetScoresAsync());

    [HttpGet("round/{roundId:int}")]
    public async Task<IActionResult> GetByRound(int roundId)
        => Ok(await scoreService.GetScoresByRoundAsync(roundId));

    [HttpGet("team/{teamId:int}")]
    public async Task<IActionResult> GetByTeam(int teamId)
        => Ok(await scoreService.GetScoresByTeamAsync(teamId));

    [HttpGet("rounds")]
    public async Task<IActionResult> GetRounds()
        => Ok(await scoreService.GetRoundsAsync());

    [HttpPost("rounds")]
    public async Task<IActionResult> AddRound([FromBody] int number)
    {
        var round = await scoreService.AddRoundAsync(number);
        return CreatedAtAction(nameof(GetRounds), round);
    }

    [HttpPost]
    public async Task<IActionResult> AddScore([FromBody] AddScoreRequest request)
    {
        try
        {
            var score = await scoreService.AddScoreAsync(
                request.TeamId, request.RoundId, request.ObjectiveId, request.Points);
            return CreatedAtAction(nameof(GetAll), new { id = score.Id }, score);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var removed = await scoreService.RemoveScoreAsync(id);
        return removed ? NoContent() : NotFound();
    }
}

public record AddScoreRequest(int TeamId, int RoundId, int ObjectiveId, int Points);
