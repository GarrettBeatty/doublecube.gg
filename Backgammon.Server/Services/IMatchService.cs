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
    /// Create a new match between two players
    /// </summary>
    Task<Match> CreateMatchAsync(string player1Id, string player2Id, int targetScore);

    /// <summary>
    /// Get a match by ID
    /// </summary>
    Task<Match?> GetMatchAsync(string matchId);

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
    /// Abandon a match
    /// </summary>
    Task AbandonMatchAsync(string matchId, string abandoningPlayerId);

    /// <summary>
    /// Get match statistics for a player
    /// </summary>
    Task<MatchStats> GetPlayerMatchStatsAsync(string playerId);

    /// <summary>
    /// Create a match lobby without starting the first game
    /// </summary>
    Task<Match> CreateMatchLobbyAsync(string player1Id, int targetScore, string opponentType, bool isOpenLobby, string? player1DisplayName = null, string? player2Id = null);

    /// <summary>
    /// Join an open lobby match
    /// </summary>
    Task<Match> JoinOpenLobbyAsync(string matchId, string player2Id, string? player2DisplayName = null);

    /// <summary>
    /// Start the first game in a match lobby
    /// </summary>
    Task<ServerGame> StartMatchFirstGameAsync(string matchId);

    /// <summary>
    /// Start the first game in a match lobby using an existing match object (avoids DB reload)
    /// </summary>
    Task<ServerGame> StartMatchFirstGameAsync(Match match);

    /// <summary>
    /// Leave a match lobby
    /// </summary>
    Task LeaveMatchLobbyAsync(string matchId, string playerId);

    /// <summary>
    /// Get match lobby status
    /// </summary>
    Task<Match?> GetMatchLobbyAsync(string matchId);
}
