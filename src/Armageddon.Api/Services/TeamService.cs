using Armageddon.Abstractions.Interfaces;
using Armageddon.Abstractions.Models;
using Armageddon.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Armageddon.Api.Services;

public class TeamService(ArmageddonDbContext context) : ITeamService
{
    public async Task<IEnumerable<Team>> GetAllTeamsAsync()
        => await context.Teams.AsNoTracking().ToListAsync();

    public async Task<Team?> GetTeamByIdAsync(int id)
        => await context.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Team> AddTeamAsync(string name)
    {
        var team = new Team { Name = name };
        context.Teams.Add(team);
        await context.SaveChangesAsync();
        return team;
    }

    public async Task<bool> RemoveTeamAsync(int id)
    {
        var team = await context.Teams.FindAsync(id);
        if (team is null) return false;
        context.Teams.Remove(team);
        await context.SaveChangesAsync();
        return true;
    }
}
