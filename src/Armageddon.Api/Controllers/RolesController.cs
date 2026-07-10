using Armageddon.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    public record RoleDto(string Id, string Name);
    public record CreateRoleRequest(string Name);

    [HttpGet]
    public IActionResult GetAll()
    {
        var roles = _roleManager.Roles.Select(r => new RoleDto(r.Id, r.Name ?? string.Empty)).ToList();
        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name required");
        var role = new IdentityRole(req.Name);
        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return CreatedAtAction(nameof(GetAll), new { id = role.Id }, new RoleDto(role.Id, role.Name ?? string.Empty));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null) return NotFound();
        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        return NoContent();
    }
}
