using Armageddon.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public record UserDto(string Id, string UserName, string? Email, bool MustChangePassword);
    public record CreateUserRequest(string UserName, string Email, string Password, bool MustChangePassword);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = _userManager.Users.Select(u => u.ToDto()).ToList();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        return Ok(user.ToDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var user = new ApplicationUser { UserName = req.UserName, Email = req.Email, MustChangePassword = req.MustChangePassword };
        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return CreatedAtAction(nameof(Get), new { id = user.Id }, user.ToDto());
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();
        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return NoContent();
    }
}
