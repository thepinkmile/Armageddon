using Armageddon.Abstractions.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Armageddon.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController(ISettingService settingService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await settingService.GetAllAsync());

    [HttpGet("{name}")]
    public async Task<IActionResult> GetByName(string name)
    {
        var setting = await settingService.GetByNameAsync(name);
        return setting is null ? NotFound() : Ok(setting);
    }

    [HttpPut("{name}")]
    public async Task<IActionResult> Update(string name, [FromBody] UpdateSettingRequest request)
    {
        var setting = await settingService.UpdateAsync(name, request.Value);
        return Ok(setting);
    }
}

public record UpdateSettingRequest(string Value);
