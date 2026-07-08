namespace Armageddon.Abstractions.Models;

public class Score
{
    public int Id { get; set; }
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int RoundId { get; set; }
    public Round Round { get; set; } = null!;
    public int ObjectiveId { get; set; }
    public Objective Objective { get; set; } = null!;
    public int Points { get; set; }
}
