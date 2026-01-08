using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service interface for managing correspondence (async) games
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

    /// <summary>
    /// Create a new correspondence match
    /// </summary>
    Task<(Match Match, Game FirstGame)> CreateCorrespondenceMatchAsync(
        string player1Id,
        int targetScore,
        int timePerMoveDays,
        string opponentType,
        string? player1DisplayName = null,
        string? player2Id = null,
        bool isRated = true);

    /// <summary>
    /// Handle turn completion in a correspondence game
    /// Updates the current turn player and deadline
    /// </summary>
    Task HandleTurnCompletedAsync(string matchId, string nextPlayerId);

    /// <summary>
    /// Handle timeout - forfeit the game for the player who ran out of time
    /// </summary>
    Task HandleTimeoutAsync(string matchId);
}

/// <summary>
/// DTO for correspondence game information
/// </summary>
public class CorrespondenceGameDto
{
    public string MatchId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string OpponentId { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public int OpponentRating { get; set; }
    public bool IsYourTurn { get; set; }
    public int TimePerMoveDays { get; set; }
    public DateTime? TurnDeadline { get; set; }
    public TimeSpan? TimeRemaining { get; set; }
    public int MoveCount { get; set; }
    public string MatchScore { get; set; } = string.Empty;
    public int TargetScore { get; set; }
    public bool IsRated { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// Response containing both "your turn" and "waiting" games
/// </summary>
public class CorrespondenceGamesResponse
{
    public List<CorrespondenceGameDto> YourTurnGames { get; set; } = new();
    public List<CorrespondenceGameDto> WaitingGames { get; set; } = new();
    public int TotalYourTurn { get; set; }
    public int TotalWaiting { get; set; }
}
