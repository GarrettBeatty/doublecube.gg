using Backgammon.Server.Models;
using Match = Backgammon.Server.Models.Match;

namespace Backgammon.Server.Services;

/// <summary>
/// Factory interface for creating and configuring GameSession instances.
/// </summary>
public interface IGameSessionFactory
{
    /// <summary>
    /// Creates a new game session for a match game.
    /// </summary>
    GameSession CreateMatchGameSession(Match match, string gameId);

    /// <summary>
    /// Creates a new game session for analysis mode.
    /// </summary>
    GameSession CreateAnalysisSession(string gameId);

    /// <summary>
    /// Initializes a newly created session (rolls dice, starts game).
    /// </summary>
    void InitializeSession(GameSession session);
}
