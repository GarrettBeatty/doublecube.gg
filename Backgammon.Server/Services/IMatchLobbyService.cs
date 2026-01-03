using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing match lobby operations
/// </summary>
public interface IMatchLobbyService
{
    /// <summary>
    /// Create a new match lobby with configuration
    /// </summary>
    Task<Match> CreateMatchLobbyAsync(string playerId, MatchConfig config, string? displayName);

    /// <summary>
    /// Join a match lobby
    /// </summary>
    Task<(bool Success, Match? Match, string? Error)> JoinMatchLobbyAsync(string matchId, string playerId, string? displayName);

    /// <summary>
    /// Start the first game in a match lobby
    /// </summary>
    Task<(bool Success, Game? Game, Match? Match, string? Error)> StartMatchGameAsync(string matchId, string playerId);

    /// <summary>
    /// Leave a match lobby
    /// </summary>
    Task<bool> LeaveMatchLobbyAsync(string matchId, string playerId);

    /// <summary>
    /// Get match lobby status
    /// </summary>
    Task<Match?> GetMatchLobbyAsync(string matchId);

    /// <summary>
    /// Start a match with an AI opponent
    /// </summary>
    Task<(Game Game, Match Match)?> StartMatchWithAiAsync(Match match);
}
