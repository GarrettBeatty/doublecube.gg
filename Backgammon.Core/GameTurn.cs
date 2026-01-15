namespace Backgammon.Core;

/// <summary>
/// Represents a single turn in a backgammon game
/// </summary>
public class GameTurn
{
    /// <summary>
    /// Turn number (1-based)
    /// </summary>
    public int TurnNumber { get; set; }

    /// <summary>
    /// Which player's turn
    /// </summary>
    public CheckerColor Player { get; set; }

    /// <summary>
    /// First die value (1-6)
    /// </summary>
    public int Die1 { get; set; }

    /// <summary>
    /// Second die value (1-6)
    /// </summary>
    public int Die2 { get; set; }

    /// <summary>
    /// Moves made this turn (in order)
    /// </summary>
    public List<Move> Moves { get; set; } = new();

    /// <summary>
    /// Cube action taken this turn (if any)
    /// </summary>
    public CubeAction? CubeAction { get; set; }

    /// <summary>
    /// Position SGF before this turn (for instant replay to this point)
    /// </summary>
    public string? PositionSgf { get; set; }
}
