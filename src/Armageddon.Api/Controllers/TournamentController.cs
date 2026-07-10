using Armageddon.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[ApiController]
[Route("api/tournament")]
public class TournamentController(ITournamentService tournamentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
        => Ok(await tournamentService.GetOrCreateAsync());

    [HttpPost("randomise")]
    public async Task<IActionResult> Randomise()
    {
        try { return Ok(await tournamentService.RandomiseAsync()); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start()
    {
        try { return Ok(await tournamentService.StartAsync()); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("matches")]
    public async Task<IActionResult> GetMatches()
        => Ok(await tournamentService.GetMatchesAsync());

    [HttpPost("matches/{id:int}/start")]
    public async Task<IActionResult> StartMatch(int id)
    {
        try { return Ok(await tournamentService.StartMatchAsync(id)); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("matches/{id:int}/complete")]
    public async Task<IActionResult> CompleteMatch(int id)
    {
        try { return Ok(await tournamentService.CompleteMatchAsync(id)); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        await tournamentService.ResetAsync();
        return NoContent();
    }

    [HttpPost("finalise")]
    public async Task<IActionResult> Finalise()
    {
        try { return Ok(await tournamentService.FinaliseTournamentAsync()); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("results")]
    public async Task<IActionResult> GetResults()
        => Ok(await tournamentService.GetResultsAsync());
}
