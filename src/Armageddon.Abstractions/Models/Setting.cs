namespace Armageddon.Abstractions.Models;

public class Setting
{
    public int Id { get; set; }

    /// <summary>Unique setting name, e.g. "MatchDurationMinutes".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Setting value stored as a string.</summary>
    public string Value { get; set; } = string.Empty;
}
