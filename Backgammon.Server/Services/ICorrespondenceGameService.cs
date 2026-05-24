using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Read-only query facade for correspondence games. After Phase 4b, mutations
/// (create match, handle turn/timeout/init) live on <see cref="Grains.Interfaces.IMatchGrain"/>;
/// the methods left here just batch repository reads for the home/lobby pages.
/// </summary>
public interface ICorrespondenceGameService
{
    /// <summary>
    /// Get correspondence games where it's the player's turn
    /// </summary>
    Task<List<CorrespondenceGameDto>> GetMyTurnGamesAsync(string playerId);

    /// <summary>
    /// Get correspondence games where the player is waiting for opponent
    /// </summary>
    Task<List<CorrespondenceGameDto>> GetWaitingGamesAsync(string playerId);

    /// <summary>
    /// Get all correspondence games for a player (both turn types)
    /// </summary>
    Task<CorrespondenceGamesResponse> GetAllCorrespondenceGamesAsync(string playerId);
}
