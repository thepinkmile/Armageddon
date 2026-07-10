namespace Armageddon.Abstractions.Models;

public enum ObjectiveType
{
    /// <summary>Can be scored at most once per team per game (or up to MaxUsage times).</summary>
    OneTime,

    /// <summary>Can be scored many times per team per game (bounded by MaxUsage when set).</summary>
    Recurring
}
