namespace Armageddon.Abstractions.Models;

public class Tournament
{
    public int Id { get; set; }
    public TournamentStatus Status { get; set; } = TournamentStatus.NotStarted;
    public ICollection<Match> Matches { get; set; } = [];
}
