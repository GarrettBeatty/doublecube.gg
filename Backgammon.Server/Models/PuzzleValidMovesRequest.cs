using System.Text.Json.Serialization;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Request DTO for getting valid moves in a puzzle position with pending moves applied.
/// </summary>
[TranspilationSource]
public class PuzzleValidMovesRequest
{
    /// <summary>
    /// Board state (24 points).
    /// </summary>
    [JsonPropertyName("boardState")]
    public List<PointStateDto> BoardState { get; set; } = new();

    /// <summary>
    /// Current player to move: "White" or "Red".
    /// </summary>
    [JsonPropertyName("currentPlayer")]
    public string CurrentPlayer { get; set; } = "White";

    /// <summary>
    /// The dice roll [die1, die2].
    /// </summary>
    [JsonPropertyName("dice")]
    public int[] Dice { get; set; } = new int[2];

    /// <summary>
    /// Number of white checkers on the bar.
    /// </summary>
    [JsonPropertyName("whiteCheckersOnBar")]
    public int WhiteCheckersOnBar { get; set; }

    /// <summary>
    /// Number of red checkers on the bar.
    /// </summary>
    [JsonPropertyName("redCheckersOnBar")]
    public int RedCheckersOnBar { get; set; }

    /// <summary>
    /// Number of white checkers borne off.
    /// </summary>
    [JsonPropertyName("whiteBornOff")]
    public int WhiteBornOff { get; set; }

    /// <summary>
    /// Number of red checkers borne off.
    /// </summary>
    [JsonPropertyName("redBornOff")]
    public int RedBornOff { get; set; }

    /// <summary>
    /// Moves already applied in the puzzle (to track remaining dice).
    /// </summary>
    [JsonPropertyName("pendingMoves")]
    public List<MoveDto> PendingMoves { get; set; } = new();
}
