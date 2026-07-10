using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface ITeamService
{
    Task<IEnumerable<Team>> GetAllTeamsAsync();
    Task<Team?> GetTeamByIdAsync(int id);
    Task<Team> AddTeamAsync(string name);
    Task<bool> RemoveTeamAsync(int id);
}
