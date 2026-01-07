using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Cosmos DB document for storing both in-progress and completed games.
/// Stores complete game state for reconstruction after server restart.
/// Wraps Core.Game domain object and adds server/infrastructure metadata.
/// </summary>
public class Game
{
    /// <summary>
    /// Core domain object containing pure game outcome data
    /// </summary>
    [JsonPropertyName("coreGame")]
    public Core.Game CoreGame { get; set; } = new();

    /// <summary>
    /// Cosmos DB document id - uses gameId as the unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Player information

    /// <summary>
    /// Player ID for white (starting player)
    /// </summary>
    [JsonPropertyName("whitePlayerId")]
    public string? WhitePlayerId { get; set; }

    /// <summary>
    /// Player ID for red
    /// </summary>
    [JsonPropertyName("redPlayerId")]
    public string? RedPlayerId { get; set; }

    /// <summary>
    /// Registered user ID for white player (null if anonymous)
    /// </summary>
    [JsonPropertyName("whiteUserId")]
    public string? WhiteUserId { get; set; }

    /// <summary>
    /// Registered user ID for red player (null if anonymous)
    /// </summary>
    [JsonPropertyName("redUserId")]
    public string? RedUserId { get; set; }

    /// <summary>
    /// Display name for white player at time of game
    /// </summary>
    [JsonPropertyName("whitePlayerName")]
    public string? WhitePlayerName { get; set; }

    /// <summary>
    /// Display name for red player at time of game
    /// </summary>
    [JsonPropertyName("redPlayerName")]
    public string? RedPlayerName { get; set; }

    // Game status (delegates to CoreGame via convenience property below)

    /// <summary>
    /// Whether game has been started (both players joined and first roll happened)
    /// </summary>
    [JsonPropertyName("gameStarted")]
    public bool GameStarted { get; set; }

    // Board state

    /// <summary>
    /// Array of 24 point states (positions 1-24)
    /// Each element: { position: int, color: string?, count: int }
    /// </summary>
    [JsonPropertyName("boardState")]
    public List<PointStateDto> BoardState { get; set; } = new();

    // Player states

    /// <summary>
    /// Number of white checkers on the bar
    /// </summary>
    [JsonPropertyName("whiteCheckersOnBar")]
    public int WhiteCheckersOnBar { get; set; }

    /// <summary>
    /// Number of red checkers on the bar
    /// </summary>
    [JsonPropertyName("redCheckersOnBar")]
    public int RedCheckersOnBar { get; set; }

    /// <summary>
    /// Number of white checkers borne off
    /// </summary>
    [JsonPropertyName("whiteBornOff")]
    public int WhiteBornOff { get; set; }

    /// <summary>
    /// Number of red checkers borne off
    /// </summary>
    [JsonPropertyName("redBornOff")]
    public int RedBornOff { get; set; }

    // Current turn state

    /// <summary>
    /// Current player color: "White" or "Red"
    /// </summary>
    [JsonPropertyName("currentPlayer")]
    public string CurrentPlayer { get; set; } = "White";

    // Dice state

    /// <summary>
    /// First die value (0 if not rolled yet)
    /// </summary>
    [JsonPropertyName("die1")]
    public int Die1 { get; set; }

    /// <summary>
    /// Second die value (0 if not rolled yet)
    /// </summary>
    [JsonPropertyName("die2")]
    public int Die2 { get; set; }

    /// <summary>
    /// Remaining die values that can be used this turn
    /// </summary>
    [JsonPropertyName("remainingMoves")]
    public List<int> RemainingMoves { get; set; } = new();

    // Doubling cube

    /// <summary>
    /// Current value of the doubling cube (1, 2, 4, 8, 16, 32, 64)
    /// </summary>
    [JsonPropertyName("doublingCubeValue")]
    public int DoublingCubeValue { get; set; } = 1;

    /// <summary>
    /// Owner of the doubling cube: null (centered), "White", or "Red"
    /// </summary>
    [JsonPropertyName("doublingCubeOwner")]
    public string? DoublingCubeOwner { get; set; }

    // Completion data (delegates to CoreGame via convenience properties below)

    /// <summary>
    /// Array of moves in notation "from/to" (e.g., ["24/20", "13/9", "bar/24", "6/off"])
    /// Preserves order and allows full game replay (for DynamoDB serialization)
    /// </summary>
    [JsonPropertyName("moves")]
    public List<string> Moves { get; set; } = new();

    /// <summary>
    /// Total number of moves executed in the game
    /// </summary>
    [JsonPropertyName("moveCount")]
    public int MoveCount { get; set; }

    /// <summary>
    /// Complete turn-by-turn history for game analysis and replay.
    /// Each turn includes position SGF, dice rolled, moves made, and doubling actions.
    /// </summary>
    [JsonPropertyName("turnHistory")]
    public List<TurnSnapshotDto> TurnHistory { get; set; } = new();

    // Timestamps

    /// <summary>
    /// When the game was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the game was last updated (last move, dice roll, etc.)
    /// </summary>
    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the game ended (null if still in progress)
    /// </summary>
    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Game duration in seconds (calculated when game completes)
    /// </summary>
    [JsonPropertyName("durationSeconds")]
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Whether this game is against an AI opponent
    /// </summary>
    [JsonPropertyName("isAiOpponent")]
    public bool IsAiOpponent { get; set; }

    /// <summary>
    /// Whether this game affects player ratings (rated vs unrated)
    /// </summary>
    [JsonPropertyName("isRated")]
    public bool IsRated { get; set; } = true;

    /// <summary>
    /// White player's rating before the game
    /// </summary>
    [JsonPropertyName("whiteRatingBefore")]
    public int? WhiteRatingBefore { get; set; }

    /// <summary>
    /// Red player's rating before the game
    /// </summary>
    [JsonPropertyName("redRatingBefore")]
    public int? RedRatingBefore { get; set; }

    /// <summary>
    /// White player's rating after the game
    /// </summary>
    [JsonPropertyName("whiteRatingAfter")]
    public int? WhiteRatingAfter { get; set; }

    /// <summary>
    /// Red player's rating after the game
    /// </summary>
    [JsonPropertyName("redRatingAfter")]
    public int? RedRatingAfter { get; set; }

    // Convenience properties that delegate to CoreGame
    // These maintain backward compatibility with existing code

    /// <summary>
    /// Unique game session ID (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public string GameId
    {
        get => CoreGame.GameId;
        set => CoreGame.GameId = value;
    }

    /// <summary>
    /// Game status (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public string Status
    {
        get => CoreGame.Status.ToString();
        set => CoreGame.Status = Enum.Parse<Core.GameStatus>(value);
    }

    /// <summary>
    /// Winning player color (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public string? Winner
    {
        get => CoreGame.Winner?.ToString();
        set => CoreGame.Winner = value != null ? Enum.Parse<Core.CheckerColor>(value) : null;
    }

    /// <summary>
    /// Game result stakes (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public int Stakes
    {
        get => CoreGame.Stakes;
        set => CoreGame.Stakes = value;
    }

    /// <summary>
    /// Win type (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public string? WinType
    {
        get => CoreGame.WinType.ToString();
        set => CoreGame.WinType = value != null ? Enum.Parse<Core.WinType>(value) : Core.WinType.Normal;
    }

    /// <summary>
    /// Match ID (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public string? MatchId
    {
        get => CoreGame.MatchId;
        set => CoreGame.MatchId = value;
    }

    /// <summary>
    /// Whether this is a match game (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public bool IsMatchGame
    {
        get => CoreGame.IsMatchGame;
        set => CoreGame.IsMatchGame = value;
    }

    /// <summary>
    /// Whether Crawford rule applies (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public bool IsCrawfordGame
    {
        get => CoreGame.IsCrawfordGame;
        set => CoreGame.IsCrawfordGame = value;
    }

    /// <summary>
    /// Move history (delegates to CoreGame)
    /// </summary>
    [JsonIgnore]
    public List<Core.Move> MoveHistory
    {
        get => CoreGame.MoveHistory;
        set => CoreGame.MoveHistory = value;
    }
}
