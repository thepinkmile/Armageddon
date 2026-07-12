namespace Armageddon.Abstractions.Models;

public class Match
{
    public int Id { get; set; }

    /// <summary>Which knockout round this match belongs to (1 = first round, 2 = semi, etc.).</summary>
    public int RoundNumber { get; set; }

    /// <summary>Position slot within the round (0-based). Used to pair winners into the next round.</summary>
    public int Slot { get; set; }

    public int? TeamAId { get; set; }
    public Team? TeamA { get; set; }

    public int? TeamBId { get; set; }
    public Team? TeamB { get; set; }

    public int? WinnerId { get; set; }
    public Team? Winner { get; set; }

    /// <summary>DB Round used to record scores for this match. Null until the match is played.</summary>
    public int? RoundId { get; set; }
    public Round? Round { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Pending;

    /// <summary>
    /// True when this is a qualifier playoff match created to resolve a tie between
    /// teams competing for a single open slot in the next main round.
    /// </summary>
    public bool IsPlayoff { get; set; }

    public int TournamentId { get; set; }
    public Tournament Tournament { get; set; } = null!;
}
