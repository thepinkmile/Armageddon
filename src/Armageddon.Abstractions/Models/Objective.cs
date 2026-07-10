namespace Armageddon.Abstractions.Models;

public class Objective
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Points awarded each time this objective is scored.</summary>
    public int Points { get; set; } = 100;

    /// <summary>
    /// Maximum number of times this objective may be scored per team per match.
    /// <c>null</c>, <c>0</c>, or negative means unlimited (Recurring).
    /// <c>1</c> means one-time. Greater than 1 means limited to that count.
    /// </summary>
    public int? MaxUsage { get; set; }

    public ICollection<Score> Scores { get; set; } = [];

    /// <summary>Returns a human-readable label for the usage limit.</summary>
    public string UsageLabel => MaxUsage is null or <= 0
        ? "Recurring"
        : MaxUsage == 1
            ? "one-time"
            : $"limited: {MaxUsage}";
}
