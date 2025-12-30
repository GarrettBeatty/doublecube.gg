namespace Backgammon.Core;

/// <summary>
/// Represents a complete turn in the game, including dice rolled and all moves made
/// </summary>
public class TurnRecord
{
    /// <summary>
    /// Sequential turn number (starts at 1)
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Which player made this turn
    /// </summary>
    public CheckerColor Player { get; set; }

    /// <summary>
    /// Dice values rolled for this turn (length 2 for normal rolls, length 4 for doubles)
    /// </summary>
    public int[] DiceRolled { get; set; } = Array.Empty<int>();

    /// <summary>
    /// All moves executed during this turn
    /// </summary>
    public List<Move> Moves { get; set; } = new();

    /// <summary>
    /// When this turn was started (when dice were rolled)
    /// </summary>
    public DateTime Timestamp { get; set; }
}
