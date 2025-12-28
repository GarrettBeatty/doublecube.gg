using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backgammon.Server.Models;

/// <summary>
/// MongoDB document for storing both in-progress and completed games.
/// Stores complete game state for reconstruction after server restart.
/// </summary>
public class Game
{
    /// <summary>
    /// MongoDB ObjectId - auto-generated unique identifier
    /// </summary>
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Unique game session ID from GameSession
    /// </summary>
    [BsonElement("gameId")]
    public string GameId { get; set; } = string.Empty;

    // Player information
    /// <summary>
    /// Player ID for white (starting player)
    /// </summary>
    [BsonElement("whitePlayerId")]
    public string? WhitePlayerId { get; set; }

    /// <summary>
    /// Player ID for red
    /// </summary>
    [BsonElement("redPlayerId")]
    public string? RedPlayerId { get; set; }

    /// <summary>
    /// Registered user ID for white player (null if anonymous)
    /// </summary>
    [BsonElement("whiteUserId")]
    public string? WhiteUserId { get; set; }

    /// <summary>
    /// Registered user ID for red player (null if anonymous)
    /// </summary>
    [BsonElement("redUserId")]
    public string? RedUserId { get; set; }

    /// <summary>
    /// Display name for white player at time of game
    /// </summary>
    [BsonElement("whitePlayerName")]
    public string? WhitePlayerName { get; set; }

    /// <summary>
    /// Display name for red player at time of game
    /// </summary>
    [BsonElement("redPlayerName")]
    public string? RedPlayerName { get; set; }

    // Game status
    /// <summary>
    /// Game state: "InProgress", "Completed", or "Abandoned"
    /// </summary>
    [BsonElement("status")]
    public string Status { get; set; } = "InProgress";

    /// <summary>
    /// Whether game has been started (both players joined and first roll happened)
    /// </summary>
    [BsonElement("gameStarted")]
    public bool GameStarted { get; set; }

    // Board state
    /// <summary>
    /// Array of 24 point states (positions 1-24)
    /// Each element: { position: int, color: string?, count: int }
    /// </summary>
    [BsonElement("boardState")]
    public List<PointStateDto> BoardState { get; set; } = new();

    // Player states
    /// <summary>
    /// Number of white checkers on the bar
    /// </summary>
    [BsonElement("whiteCheckersOnBar")]
    public int WhiteCheckersOnBar { get; set; }

    /// <summary>
    /// Number of red checkers on the bar
    /// </summary>
    [BsonElement("redCheckersOnBar")]
    public int RedCheckersOnBar { get; set; }

    /// <summary>
    /// Number of white checkers borne off
    /// </summary>
    [BsonElement("whiteBornOff")]
    public int WhiteBornOff { get; set; }

    /// <summary>
    /// Number of red checkers borne off
    /// </summary>
    [BsonElement("redBornOff")]
    public int RedBornOff { get; set; }

    // Current turn state
    /// <summary>
    /// Current player color: "White" or "Red"
    /// </summary>
    [BsonElement("currentPlayer")]
    public string CurrentPlayer { get; set; } = "White";

    // Dice state
    /// <summary>
    /// First die value (0 if not rolled yet)
    /// </summary>
    [BsonElement("die1")]
    public int Die1 { get; set; }

    /// <summary>
    /// Second die value (0 if not rolled yet)
    /// </summary>
    [BsonElement("die2")]
    public int Die2 { get; set; }

    /// <summary>
    /// Remaining die values that can be used this turn
    /// </summary>
    [BsonElement("remainingMoves")]
    public List<int> RemainingMoves { get; set; } = new();

    // Doubling cube
    /// <summary>
    /// Current value of the doubling cube (1, 2, 4, 8, 16, 32, 64)
    /// </summary>
    [BsonElement("doublingCubeValue")]
    public int DoublingCubeValue { get; set; } = 1;

    /// <summary>
    /// Owner of the doubling cube: null (centered), "White", or "Red"
    /// </summary>
    [BsonElement("doublingCubeOwner")]
    public string? DoublingCubeOwner { get; set; }

    // Move history
    /// <summary>
    /// Array of moves in notation "from/to" (e.g., ["24/20", "13/9", "bar/24", "6/off"])
    /// Preserves order and allows full game replay
    /// </summary>
    [BsonElement("moves")]
    public List<string> Moves { get; set; } = new();

    // Completion data
    /// <summary>
    /// Winning player color: "White" or "Red" (null if game not complete)
    /// </summary>
    [BsonElement("winner")]
    public string? Winner { get; set; }

    /// <summary>
    /// Game result stakes: 1=normal, 2=gammon, 3=backgammon
    /// Includes doubling cube value
    /// </summary>
    [BsonElement("stakes")]
    public int Stakes { get; set; }

    /// <summary>
    /// Total number of moves executed in the game
    /// </summary>
    [BsonElement("moveCount")]
    public int MoveCount { get; set; }

    // Timestamps
    /// <summary>
    /// When the game was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the game was last updated (last move, dice roll, etc.)
    /// </summary>
    [BsonElement("lastUpdatedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the game ended (null if still in progress)
    /// </summary>
    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Game duration in seconds (calculated when game completes)
    /// </summary>
    [BsonElement("durationSeconds")]
    public int DurationSeconds { get; set; }
}

/// <summary>
/// Represents a single point on the backgammon board for storage
/// </summary>
public class PointStateDto
{
    /// <summary>
    /// Point position (1-24)
    /// </summary>
    [BsonElement("position")]
    public int Position { get; set; }

    /// <summary>
    /// Color of checkers on this point: null (empty), "White", or "Red"
    /// </summary>
    [BsonElement("color")]
    public string? Color { get; set; }

    /// <summary>
    /// Number of checkers on this point
    /// </summary>
    [BsonElement("count")]
    public int Count { get; set; }
}
