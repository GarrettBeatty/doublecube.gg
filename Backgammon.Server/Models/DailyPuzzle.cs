using System.Text.Json.Serialization;

namespace Backgammon.Server.Models;

/// <summary>
/// Entity model for a daily puzzle stored in DynamoDB.
/// Contains the position, dice roll, and optimal solution.
/// </summary>
public class DailyPuzzle
{
    /// <summary>
    /// Unique puzzle identifier
    /// </summary>
    [JsonPropertyName("puzzleId")]
    public string PuzzleId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Date of the puzzle in yyyy-MM-dd format (e.g., "2026-01-10")
    /// </summary>
    [JsonPropertyName("puzzleDate")]
    public string PuzzleDate { get; set; } = string.Empty;

    /// <summary>
    /// Position encoded in SGF format for reconstruction
    /// </summary>
    [JsonPropertyName("positionSgf")]
    public string PositionSgf { get; set; } = string.Empty;

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
    /// The optimal move sequence as determined by the evaluator
    /// </summary>
    [JsonPropertyName("bestMoves")]
    public List<MoveDto> BestMoves { get; set; } = new();

    /// <summary>
    /// Best moves in standard notation (e.g., "24/20 13/11")
    /// </summary>
    [JsonPropertyName("bestMovesNotation")]
    public string BestMovesNotation { get; set; } = string.Empty;

    /// <summary>
    /// Equity of the best move sequence
    /// </summary>
    [JsonPropertyName("bestMoveEquity")]
    public double BestMoveEquity { get; set; }

    /// <summary>
    /// Alternative move sequences within the equity tolerance that are also accepted
    /// </summary>
    [JsonPropertyName("alternativeMoves")]
    public List<AlternativeMove> AlternativeMoves { get; set; } = new();

    /// <summary>
    /// Evaluator used to analyze this puzzle ("Gnubg" or "Heuristic")
    /// </summary>
    [JsonPropertyName("evaluatorType")]
    public string EvaluatorType { get; set; } = string.Empty;

    /// <summary>
    /// When the puzzle was created
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of users who have solved this puzzle
    /// </summary>
    [JsonPropertyName("solvedCount")]
    public int SolvedCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of attempts on this puzzle.
    /// </summary>
    [JsonPropertyName("attemptCount")]
    public int AttemptCount { get; set; }
}
