using Backgammon.Server.Models;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Read-only query facade for matches; delegates straight to <see cref="IMatchRepository"/>.
/// Mutations live on <see cref="Grains.Interfaces.IMatchGrain"/> after Phase 4b.
/// </summary>
public class MatchService : IMatchService
{
    private readonly IMatchRepository _matchRepository;

    public MatchService(IMatchRepository matchRepository)
    {
        _matchRepository = matchRepository;
    }

    public Task<Match?> GetMatchAsync(string matchId) =>
        _matchRepository.GetMatchByIdAsync(matchId);

    public Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null) =>
        _matchRepository.GetPlayerMatchesAsync(playerId, status);

    public Task<List<Match>> GetActiveMatchesAsync() =>
        _matchRepository.GetActiveMatchesAsync();

    public Task<List<Match>> GetOpenLobbiesAsync(int limit = 50, bool? isCorrespondence = null) =>
        _matchRepository.GetOpenLobbiesAsync(limit, isCorrespondence);

    public Task<List<Match>> GetRegularLobbiesAsync(int limit = 50) =>
        _matchRepository.GetOpenLobbiesAsync(limit, isCorrespondence: false);

    public Task<List<Match>> GetCorrespondenceLobbiesAsync(int limit = 50) =>
        _matchRepository.GetOpenLobbiesAsync(limit, isCorrespondence: true);

    public Task<MatchStats> GetPlayerMatchStatsAsync(string playerId) =>
        _matchRepository.GetPlayerMatchStatsAsync(playerId);
}
