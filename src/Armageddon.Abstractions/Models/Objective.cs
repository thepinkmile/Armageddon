namespace Armageddon.Abstractions.Models;

public class Objective
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether this objective is one-time or recurring per team.</summary>
    public ObjectiveType Type { get; set; } = ObjectiveType.Recurring;

    /// <summary>
    /// Maximum number of times this objective may be scored per team.
    /// <c>null</c> or <c>-1</c> means unlimited.
    /// For <see cref="ObjectiveType.OneTime"/> objectives this is implicitly 1
    /// unless a lower or equal positive value is set here.
    /// </summary>
    public int? MaxUsage { get; set; }

    public ICollection<Score> Scores { get; set; } = [];
}
