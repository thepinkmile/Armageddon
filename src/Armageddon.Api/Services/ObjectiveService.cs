using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Services;

public class ObjectiveService(ArmageddonDbContext context) : IObjectiveService
{
    public async Task<IEnumerable<Objective>> GetAllObjectivesAsync()
        => await context.Objectives.AsNoTracking().ToListAsync();

    public async Task<Objective?> GetObjectiveByIdAsync(int id)
        => await context.Objectives.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

    public async Task<Objective> AddObjectiveAsync(string name)
    {
        var objective = new Objective { Name = name };
        context.Objectives.Add(objective);
        await context.SaveChangesAsync();
        return objective;
    }

    public async Task<bool> RemoveObjectiveAsync(int id)
    {
        var objective = await context.Objectives.FindAsync(id);
        if (objective is null) return false;
        context.Objectives.Remove(objective);
        await context.SaveChangesAsync();
        return true;
    }
}
