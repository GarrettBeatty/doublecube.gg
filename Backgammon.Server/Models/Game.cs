using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Cosmos DB document for storing both in-progress and completed games.
/// Stores complete game state for reconstruction after server restart.
/// </summary>
public class Game
{
    /// <summary>
    /// Cosmos DB document id - uses gameId as the unique identifier
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Unique game session ID from GameSession
    /// </summary>
    [JsonPropertyName("gameId")]
    public string GameId { get; set; } = string.Empty;

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

    // Game status
    /// <summary>
    /// Game state: "InProgress", "Completed", or "Abandoned"
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "InProgress";

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

    // Move history
    /// <summary>
    /// Array of moves in notation "from/to" (e.g., ["24/20", "13/9", "bar/24", "6/off"])
    /// Preserves order and allows full game replay
    /// </summary>
    [JsonPropertyName("moves")]
    public List<string> Moves { get; set; } = new();

    /// <summary>
    /// Complete turn-by-turn history with dice rolls and moves
    /// Used for game replay and history navigation
    /// </summary>
    [JsonPropertyName("turnHistory")]
    public List<TurnRecordDto> TurnHistory { get; set; } = new();

    // Completion data
    /// <summary>
    /// Winning player color: "White" or "Red" (null if game not complete)
    /// </summary>
    [JsonPropertyName("winner")]
    public string? Winner { get; set; }

    /// <summary>
    /// Game result stakes: 1=normal, 2=gammon, 3=backgammon
    /// Includes doubling cube value
    /// </summary>
    [JsonPropertyName("stakes")]
    public int Stakes { get; set; }

    /// <summary>
    /// Total number of moves executed in the game
    /// </summary>
    [JsonPropertyName("moveCount")]
    public int MoveCount { get; set; }

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
}

/// <summary>
/// Represents a single point on the backgammon board for storage
/// </summary>
public class PointStateDto
{
    /// <summary>
    /// Point position (1-24)
    /// </summary>
    [JsonPropertyName("position")]
    public int Position { get; set; }

    /// <summary>
    /// Color of checkers on this point: null (empty), "White", or "Red"
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Number of checkers on this point
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
