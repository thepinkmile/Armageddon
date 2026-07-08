namespace Armageddon.Abstractions.Models;

public class Objective
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Score> Scores { get; set; } = [];
}
