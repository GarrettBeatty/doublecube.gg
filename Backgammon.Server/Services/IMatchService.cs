using Backgammon.Core;
using Backgammon.Server.Models;
using Match = Backgammon.Server.Models.Match;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for managing matches
/// </summary>
public interface IMatchService
{
    /// <summary>
    /// Create a new match and immediately create the first game
    /// </summary>
    /// <param name="player1Id">First player ID</param>
    /// <param name="targetScore">Target score for the match (1-25)</param>
    /// <param name="opponentType">Type of opponent: "AI", "OpenLobby", or "Friend"</param>
    /// <param name="player1DisplayName">Optional display name for player 1</param>
    /// <param name="player2Id">Optional player 2 ID (for Friend matches)</param>
    /// <returns>Tuple of created Match and first ServerGame</returns>
    Task<(Match Match, ServerGame FirstGame)> CreateMatchAsync(
        string player1Id,
        int targetScore,
        string opponentType,
        string? player1DisplayName = null,
        string? player2Id = null);

    /// <summary>
    /// Get a match by ID
    /// </summary>
    Task<Match?> GetMatchAsync(string matchId);

    /// <summary>
    /// Update match data
    /// </summary>
    Task UpdateMatchAsync(Match match);

    /// <summary>
    /// Start the next game in a match
    /// </summary>
    Task<ServerGame> StartNextGameAsync(string matchId);

    /// <summary>
    /// Complete a game and update match scores
    /// </summary>
    Task CompleteGameAsync(string gameId, GameResult result);

    /// <summary>
    /// Check if a match is complete
    /// </summary>
    Task<bool> IsMatchCompleteAsync(string matchId);

    /// <summary>
    /// Get matches for a specific player
    /// </summary>
    Task<List<Match>> GetPlayerMatchesAsync(string playerId, string? status = null);

    /// <summary>
    /// Get active matches
    /// </summary>
    Task<List<Match>> GetActiveMatchesAsync();

    /// <summary>
    /// Get open lobbies waiting for opponents
    /// </summary>
    Task<List<Match>> GetOpenLobbiesAsync(int limit = 50);

    /// <summary>
    /// Abandon a match
    /// </summary>
    Task AbandonMatchAsync(string matchId, string abandoningPlayerId);

    /// <summary>
    /// Get match statistics for a player
    /// </summary>
    Task<MatchStats> GetPlayerMatchStatsAsync(string playerId);

    /// <summary>
    /// Join an existing match as player 2 (for OpenLobby and Friend matches)
    /// </summary>
    /// <param name="matchId">Match ID to join</param>
    /// <param name="player2Id">Player 2 ID</param>
    /// <param name="player2DisplayName">Optional display name for player 2</param>
    /// <returns>Updated match</returns>
    Task<Match> JoinMatchAsync(string matchId, string player2Id, string? player2DisplayName = null);
}
