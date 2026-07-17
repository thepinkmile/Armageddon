using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Services;

public class SettingService(ArmageddonDbContext context) : ISettingService
{
    public async Task<IEnumerable<Setting>> GetAllAsync()
        => await context.Settings.AsNoTracking().ToListAsync();

    public async Task<Setting?> GetByNameAsync(string name)
        => await context.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.Name == name);

    public async Task<Setting> UpdateAsync(string name, string value)
    {
        var setting = await context.Settings.FirstOrDefaultAsync(s => s.Name == name);
        if (setting is null)
        {
            setting = new Setting { Name = name, Value = value };
            context.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }
        await context.SaveChangesAsync();
        return setting;
    }
}
