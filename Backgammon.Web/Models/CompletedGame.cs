using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Backgammon.Web.Models;

/// <summary>
/// MongoDB document for storing completed backgammon games.
/// Stores moves in compact notation for efficient retrieval and analysis.
/// </summary>
public class CompletedGame
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
    
    /// <summary>
    /// Player ID for white (starting player)
    /// </summary>
    [BsonElement("whitePlayerId")]
    public string WhitePlayerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Player ID for red
    /// </summary>
    [BsonElement("redPlayerId")]
    public string RedPlayerId { get; set; } = string.Empty;
    
    /// <summary>
    /// Array of moves in notation "from/to" (e.g., ["24/20", "13/9", "bar/24"])
    /// Preserves order and allows full game replay
    /// </summary>
    [BsonElement("moves")]
    public List<string> Moves { get; set; } = new();
    
    /// <summary>
    /// Dice rolls for each turn in format "3,4" or "6,6"
    /// Parallel to turns for full game reproduction
    /// </summary>
    [BsonElement("diceRolls")]
    public List<string> DiceRolls { get; set; } = new();
    
    /// <summary>
    /// Winning player color: "White" or "Red"
    /// </summary>
    [BsonElement("winner")]
    public string? Winner { get; set; }
    
    /// <summary>
    /// Game type: 1=normal, 2=gammon, 3=backgammon
    /// </summary>
    [BsonElement("stakes")]
    public int Stakes { get; set; }
    
    /// <summary>
    /// Total number of turns (dice rolls) in the game
    /// </summary>
    [BsonElement("turnCount")]
    public int TurnCount { get; set; }
    
    /// <summary>
    /// Total number of moves executed
    /// </summary>
    [BsonElement("moveCount")]
    public int MoveCount { get; set; }
    
    /// <summary>
    /// When the game was created
    /// </summary>
    [BsonElement("createdAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the game ended
    /// </summary>
    [BsonElement("completedAt")]
    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Game duration in seconds
    /// </summary>
    [BsonElement("durationSeconds")]
    public int DurationSeconds { get; set; }
}
