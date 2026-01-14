using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for handling game and match completion.
/// </summary>
public interface IGameCompletionService
{
    /// <summary>
    /// Handles game completion, including database updates, stats, match progression, and broadcasting.
    /// </summary>
    Task HandleGameCompletionAsync(GameSession session);

    /// <summary>
    /// Creates and starts the next game in a match atomically.
    /// Adds both players (including AI), starts the engine, and broadcasts GameStart.
    /// This eliminates the "empty session" anti-pattern by doing everything in one operation.
    /// </summary>
    /// <param name="match">The match to create the next game for</param>
    /// <param name="player1Connections">Connection IDs for player 1 (human player)</param>
    /// <param name="player2Connections">Connection IDs for player 2 (empty for AI matches)</param>
    /// <returns>The fully initialized and started game session</returns>
    Task<GameSession> CreateAndStartNextMatchGameAsync(
        Match match,
        HashSet<string> player1Connections,
        HashSet<string> player2Connections);
}
