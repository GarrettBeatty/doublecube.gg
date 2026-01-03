using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Handles game creation and joining logic
/// </summary>
public interface IGameCreationService
{
    /// <summary>
    /// Join an existing game or create a new one
    /// </summary>
    Task JoinGameAsync(string connectionId, string playerId, string? displayName, string? gameId = null);

    /// <summary>
    /// Create an analysis/practice game where one player controls both sides
    /// </summary>
    Task CreateAnalysisGameAsync(string connectionId, string userId);

    /// <summary>
    /// Create a new game against an AI opponent
    /// </summary>
    Task CreateAiGameAsync(string connectionId, string playerId, string? displayName);
}
