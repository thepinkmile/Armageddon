namespace Armageddon.Abstractions.Models;

public class Round
{
    public int Id { get; set; }
    public int Number { get; set; }
    public ICollection<Score> Scores { get; set; } = [];
}
