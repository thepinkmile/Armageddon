namespace Armageddon.Abstractions.Models;

/// <summary>
/// A historical snapshot of a completed tournament.
/// All names are stored as plain strings — no FK to Team/Round —
/// so the record remains valid even after teams are deleted.
/// </summary>
public class TournamentResult
{
    public int Id { get; set; }
    public DateTime DatePlayed { get; set; } = DateTime.UtcNow;

    /// <summary>Name of the overall winner at time of finalisation.</summary>
    public string WinnerName { get; set; } = string.Empty;

    public ICollection<TournamentResultEntry> Entries { get; set; } = [];
}
