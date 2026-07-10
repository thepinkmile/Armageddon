using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface IObjectiveService
{
    Task<IEnumerable<Objective>> GetAllObjectivesAsync();
    Task<Objective?> GetObjectiveByIdAsync(int id);
    Task<Objective> AddObjectiveAsync(string name, int points = 100, int? maxUsage = null);
    Task<Objective?> UpdateObjectiveAsync(int id, string name, int points, int? maxUsage);
    Task<bool> RemoveObjectiveAsync(int id);
}
