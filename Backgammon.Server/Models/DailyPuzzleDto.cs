using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for sending daily puzzle data to the client.
/// Note: Best moves are NOT included until the user solves the puzzle.
/// </summary>
public class DailyPuzzleDto
{
    /// <summary>
    /// Unique puzzle identifier
    /// </summary>
    [JsonPropertyName("puzzleId")]
    public string PuzzleId { get; set; } = string.Empty;

    /// <summary>
    /// Date of the puzzle in yyyy-MM-dd format
    /// </summary>
    [JsonPropertyName("puzzleDate")]
    public string PuzzleDate { get; set; } = string.Empty;

    /// <summary>
    /// Current player to move: "White" or "Red"
    /// </summary>
    [JsonPropertyName("currentPlayer")]
    public string CurrentPlayer { get; set; } = "White";

    /// <summary>
    /// The dice roll for this puzzle [die1, die2]
    /// </summary>
    [JsonPropertyName("dice")]
    public int[] Dice { get; set; } = new int[2];

    /// <summary>
    /// Board state for display (24 points)
    /// </summary>
    [JsonPropertyName("boardState")]
    public List<PointStateDto> BoardState { get; set; } = new();

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

    /// <summary>
    /// Whether the current user has already solved this puzzle
    /// </summary>
    [JsonPropertyName("alreadySolved")]
    public bool AlreadySolved { get; set; }

    /// <summary>
    /// Number of attempts the user has made today
    /// </summary>
    [JsonPropertyName("attemptsToday")]
    public int AttemptsToday { get; set; }

    /// <summary>
    /// Best moves - only populated after user has solved the puzzle
    /// </summary>
    [JsonPropertyName("bestMoves")]
    public List<MoveDto>? BestMoves { get; set; }

    /// <summary>
    /// Best moves notation - only populated after user has solved the puzzle
    /// </summary>
    [JsonPropertyName("bestMovesNotation")]
    public string? BestMovesNotation { get; set; }
}
