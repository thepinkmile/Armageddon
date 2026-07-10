using Armageddon.Abstractions.Models;

namespace Armageddon.Abstractions.Interfaces;

public interface ITournamentService
{
    Task<Tournament> GetOrCreateAsync();
    Task<Tournament> RandomiseAsync();
    Task<Tournament> StartAsync();
    Task<IEnumerable<Match>> GetMatchesAsync();
    Task<Match> StartMatchAsync(int matchId);
    Task<Match> CompleteMatchAsync(int matchId);
    Task<TournamentResult> FinaliseTournamentAsync();
    Task<IEnumerable<TournamentResult>> GetResultsAsync();
    Task ResetAsync();
}
