using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface ISettingService
{
    Task<IEnumerable<Setting>> GetAllAsync();
    Task<Setting?> GetByNameAsync(string name);
    Task<Setting> UpdateAsync(string name, string value);
}
