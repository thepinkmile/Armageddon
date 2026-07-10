namespace Armageddon.Abstractions.Models;

/// <summary>
/// One team's result within a historical tournament snapshot.
/// </summary>
public class TournamentResultEntry
{
    public int Id { get; set; }
    public int TournamentResultId { get; set; }
    public TournamentResult TournamentResult { get; set; } = null!;

    /// <summary>Team name at time of finalisation.</summary>
    public string TeamName { get; set; } = string.Empty;

    /// <summary>Final position: 1 = winner, 2 = runner-up, etc.</summary>
    public int Position { get; set; }

    /// <summary>The highest knockout round number this team reached.</summary>
    public int RoundReached { get; set; }

    /// <summary>Total points scored across all matches in the tournament.</summary>
    public int TotalPoints { get; set; }
}
