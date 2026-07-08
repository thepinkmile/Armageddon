using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface IObjectiveService
{
    Task<IEnumerable<Objective>> GetAllObjectivesAsync();
    Task<Objective?> GetObjectiveByIdAsync(int id);
    Task<Objective> AddObjectiveAsync(string name);
    Task<bool> RemoveObjectiveAsync(int id);
}
