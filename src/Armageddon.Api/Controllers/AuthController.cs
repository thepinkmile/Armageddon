using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Armageddon.Api.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace Armageddon.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _configuration = configuration;
    }

    public record LoginRequest(string Identifier, string Password);
    public record LoginResponse(string Token);

    public record CurrentUserResponse(string Id, string UserName, string? Email, bool MustChangePassword, string[] Roles);

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
    public record FirstLoginRequest(string CurrentPassword, string NewPassword);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Identifier) || string.IsNullOrEmpty(request.Password))
                return BadRequest("Missing credentials");

            ApplicationUser? user = await _userManager.FindByNameAsync(request.Identifier)
                ?? await _userManager.FindByEmailAsync(request.Identifier);
            if (user is null)
            {
                return Unauthorized();
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                return Unauthorized();
            }

            // create JWT with roles
            var jwtKey = _configuration["Jwt:Key"] ?? "dev-secret-key-change-this";
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? "Armageddon.Api";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.UserName ?? string.Empty),
                new(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var r in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, r));
            }

            if (user.MustChangePassword)
            {
                claims.Add(new Claim("must_change_password", "true"));
            }

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(12),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponse(tokenString));
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.ToString(), statusCode: 500);
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Stateless JWT: client should remove token. Return OK for symmetry.
        return Ok();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        // Look up by name claim — more resilient than GetUserAsync which requires a valid Guid in NameIdentifier
        var userName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null) return Unauthorized();

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        return Ok(new CurrentUserResponse(user.Id.ToString(), user.UserName ?? string.Empty, user.Email, user.MustChangePassword, roles));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var result = await _userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));
        // Clear MustChangePassword if it was set
        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            await _userManager.UpdateAsync(user);
        }
        return Ok();
    }

    [HttpPost("first-login")]
    public async Task<IActionResult> FirstLogin([FromBody] FirstLoginRequest req)
    {
        // Authenticate with username/email + current password to allow rotation without being logged in
        var result = await _signInManager.PasswordSignInAsync(req.CurrentPassword, req.NewPassword, isPersistent: false, lockoutOnFailure: false);
        // The above use is intentionally lightweight; clients should call login then change-password flow.
        return BadRequest("Not implemented: please log in and use change-password endpoint while authenticated.");
    }
}
