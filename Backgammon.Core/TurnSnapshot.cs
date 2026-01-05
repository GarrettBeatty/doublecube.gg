namespace Backgammon.Core;

/// <summary>
/// Captures the complete state of a single turn for game analysis and replay.
/// Combines SGF position snapshot with metadata not included in SGF format.
/// </summary>
public class TurnSnapshot
{
    /// <summary>
    /// Sequential turn number (1-based)
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Player whose turn this was
    /// </summary>
    public CheckerColor Player { get; set; }

    /// <summary>
    /// Dice rolled at the start of this turn.
    /// For regular rolls: [3, 5]
    /// For doubles: [4, 4, 4, 4]
    /// </summary>
    public int[] DiceRolled { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Position in SGF format BEFORE any moves were made this turn.
    /// Allows instant position reconstruction without replaying from game start.
    /// </summary>
    public string PositionSgf { get; set; } = string.Empty;

    /// <summary>
    /// Sequence of moves executed during this turn (in order)
    /// </summary>
    public List<Move> Moves { get; set; } = new();

    /// <summary>
    /// Doubling cube action that occurred this turn, if any
    /// </summary>
    public DoublingAction? DoublingAction { get; set; }

    /// <summary>
    /// Doubling cube value at the START of this turn
    /// </summary>
    public int CubeValue { get; set; } = 1;

    /// <summary>
    /// Doubling cube owner at the START of this turn.
    /// Null means centered (available to either player).
    /// </summary>
    public string? CubeOwner { get; set; }
}
