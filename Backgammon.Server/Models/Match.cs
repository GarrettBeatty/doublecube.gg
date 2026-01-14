using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Represents a match consisting of multiple games played to a target score.
/// Wraps Core.Match domain object and adds server/infrastructure metadata.
/// </summary>
public class Match
{
    /// <summary>
    /// Backing field for CurrentGameId - stored directly to avoid hollow objects.
    /// </summary>
    private string? _currentGameIdStored;

    /// <summary>
    /// Core domain object containing pure match logic
    /// </summary>
    [JsonPropertyName("coreMatch")]
    public Core.Match CoreMatch { get; set; } = new();

    /// <summary>
    /// Player 1 display name at time of match
    /// </summary>
    [JsonPropertyName("player1Name")]
    public string Player1Name { get; set; } = string.Empty;

    /// <summary>
    /// Player 2 display name at time of match
    /// </summary>
    [JsonPropertyName("player2Name")]
    public string Player2Name { get; set; } = string.Empty;

    // Server metadata (not in Core)

    /// <summary>
    /// When the match was last updated
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Winner player ID (null if match not complete)
    /// </summary>
    [JsonPropertyName("winnerId")]
    public string? WinnerId { get; set; }

    /// <summary>
    /// Match duration in seconds (calculated when match completes)
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Opponent type: "Friend", "AI", "OpenLobby"
    /// </summary>
    [JsonPropertyName("opponentType")]
    public string OpponentType { get; set; } = "Friend";

    /// <summary>
    /// Lobby status: "WaitingForOpponent", "Ready", "InGame"
    /// </summary>
    [JsonPropertyName("lobbyStatus")]
    public string LobbyStatus { get; set; } = "WaitingForOpponent";

    /// <summary>
    /// Whether this is an open lobby (anyone can join)
    /// </summary>
    [JsonPropertyName("isOpenLobby")]
    public bool IsOpenLobby { get; set; }

    /// <summary>
    /// Whether the match affects player ratings (AI matches are always unrated)
    /// </summary>
    [JsonPropertyName("isRated")]
    public bool IsRated { get; set; } = true;

    /// <summary>
    /// Display name for Player 1 (for anonymous players)
    /// </summary>
    [JsonPropertyName("player1DisplayName")]
    public string? Player1DisplayName { get; set; }

    /// <summary>
    /// Display name for Player 2 (for anonymous players)
    /// </summary>
    [JsonPropertyName("player2DisplayName")]
    public string? Player2DisplayName { get; set; }

    // Game summaries for avoiding hollow objects

    /// <summary>
    /// Summaries of games played in this match.
    /// This is the authoritative list of games with actual data.
    /// Avoids hollow Core.Game objects.
    /// </summary>
    [JsonPropertyName("gamesSummary")]
    public List<MatchGameSummary> GamesSummary { get; set; } = new();

    // Correspondence game fields

    /// <summary>
    /// Whether this is a correspondence (async) match
    /// </summary>
    [JsonPropertyName("isCorrespondence")]
    public bool IsCorrespondence { get; set; }

    /// <summary>
    /// Time allowed per move in days (for correspondence matches)
    /// </summary>
    [JsonPropertyName("timePerMoveDays")]
    public int TimePerMoveDays { get; set; }

    /// <summary>
    /// Deadline for the current player to make a move (for correspondence matches)
    /// </summary>
    [JsonPropertyName("turnDeadline")]
    public DateTime? TurnDeadline { get; set; }

    /// <summary>
    /// ID of the player whose turn it is (for correspondence matches)
    /// </summary>
    [JsonPropertyName("currentTurnPlayerId")]
    public string? CurrentTurnPlayerId { get; set; }

    // Convenience properties that delegate to CoreMatch
    // These maintain backward compatibility with existing code

    /// <summary>
    /// Match ID (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public string MatchId
    {
        get => CoreMatch.MatchId;
        set => CoreMatch.MatchId = value;
    }

    /// <summary>
    /// Target score (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public int TargetScore
    {
        get => CoreMatch.TargetScore;
        set => CoreMatch.TargetScore = value;
    }

    /// <summary>
    /// Player 1 ID (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public string Player1Id
    {
        get => CoreMatch.Player1Id;
        set => CoreMatch.Player1Id = value;
    }

    /// <summary>
    /// Player 2 ID (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public string Player2Id
    {
        get => CoreMatch.Player2Id;
        set => CoreMatch.Player2Id = value;
    }

    /// <summary>
    /// Player 1 score (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public int Player1Score
    {
        get => CoreMatch.Player1Score;
        set => CoreMatch.Player1Score = value;
    }

    /// <summary>
    /// Player 2 score (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public int Player2Score
    {
        get => CoreMatch.Player2Score;
        set => CoreMatch.Player2Score = value;
    }

    /// <summary>
    /// Whether current game is Crawford game (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public bool IsCrawfordGame
    {
        get => CoreMatch.IsCrawfordGame;
        set => CoreMatch.IsCrawfordGame = value;
    }

    /// <summary>
    /// Whether Crawford game has been played (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public bool HasCrawfordGameBeenPlayed
    {
        get => CoreMatch.HasCrawfordGameBeenPlayed;
        set => CoreMatch.HasCrawfordGameBeenPlayed = value;
    }

    /// <summary>
    /// List of games in this match (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public List<Core.Game> Games
    {
        get => CoreMatch.Games;
        set => CoreMatch.Games = value;
    }

    /// <summary>
    /// Current active game (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public Core.Game? CurrentGame
    {
        get => CoreMatch.CurrentGame;
        set => CoreMatch.CurrentGame = value;
    }

    /// <summary>
    /// Match status (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public string Status
    {
        get => CoreMatch.Status.ToString();
        set => CoreMatch.Status = Enum.Parse<Core.MatchStatus>(value);
    }

    /// <summary>
    /// When match was created (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public DateTime CreatedAt
    {
        get => CoreMatch.CreatedAt;
        set => CoreMatch.CreatedAt = value;
    }

    /// <summary>
    /// When match was completed (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public DateTime? CompletedAt
    {
        get => CoreMatch.CompletedAt;
        set => CoreMatch.CompletedAt = value;
    }

    /// <summary>
    /// Total number of games played - computed from GamesSummary.
    /// Falls back to CoreMatch for backward compatibility.
    /// </summary>
    [JsonIgnore]
    public int TotalGamesPlayed => GamesSummary.Count > 0 ? GamesSummary.Count : CoreMatch.TotalGamesPlayed;

    /// <summary>
    /// Current game ID - stored directly to avoid hollow Core.Game objects.
    /// Also syncs with CoreMatch.CurrentGame when a full game is available.
    /// </summary>
    [JsonIgnore]
    public string? CurrentGameId
    {
        get
        {
            // Prefer stored value to avoid hollow objects
            // CoreMatch.CurrentGame may contain full game data during gameplay
            return _currentGameIdStored ?? CoreMatch.CurrentGame?.GameId;
        }

        set
        {
            _currentGameIdStored = value;
            // Don't create hollow Core.Game - CoreMatch.CurrentGame will be set
            // when a full game is loaded during gameplay
        }
    }

    /// <summary>
    /// List of game IDs - computed from GamesSummary to avoid hollow objects.
    /// Falls back to CoreMatch.Games for backward compatibility during migration.
    /// </summary>
    [JsonIgnore]
    public List<string> GameIds
    {
        get
        {
            // Prefer GamesSummary (has actual data) over CoreMatch.Games (may be hollow)
            if (GamesSummary.Count > 0)
            {
                return GamesSummary.Select(g => g.GameId).ToList();
            }

            // Fallback for migration: use CoreMatch.Games if GamesSummary not yet populated
            return CoreMatch.Games.Select(g => g.GameId).ToList();
        }

        set
        {
            // During migration, populate GamesSummary from IDs
            // This creates minimal summaries that can be enriched later
            GamesSummary = value.Select(id => new MatchGameSummary { GameId = id }).ToList();
            // Don't set CoreMatch.Games to avoid hollow objects
        }
    }

    /// <summary>
    /// Time control configuration (delegates to CoreMatch)
    /// </summary>
    [JsonIgnore]
    public Core.TimeControlConfig TimeControl
    {
        get => CoreMatch.TimeControl;
        set => CoreMatch.TimeControl = value;
    }
}
