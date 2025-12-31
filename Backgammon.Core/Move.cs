namespace Backgammon.Core;

/// <summary>
/// Represents a single checker move
/// </summary>
public class Move
{
    /// <summary>
    /// Starting point (0 = bar, 25 = bear off)
    /// </summary>
    public int From { get; set; }

    /// <summary>
    /// Destination point (0 = bar, 25 = bear off)
    /// </summary>
    public int To { get; set; }

    /// <summary>
    /// The die value used for this move
    /// </summary>
    public int DieValue { get; set; }

    /// <summary>
    /// Whether this move hits an opponent's blot
    /// </summary>
    public bool IsHit { get; set; }

    /// <summary>
    /// Opponent's checkers on bar before this move (for undo)
    /// </summary>
    public int OpponentCheckersOnBarBefore { get; set; }

    /// <summary>
    /// Current player's checkers born off before this move (for undo)
    /// </summary>
    public int CurrentPlayerBornOffBefore { get; set; }

    /// <summary>
    /// Whether this is a bear-off move
    /// </summary>
    public bool IsBearOff => To == 0 || To == 25;

    public Move(int from, int to, int dieValue, bool isHit = false)
    {
        From = from;
        To = to;
        DieValue = dieValue;
        IsHit = isHit;
    }

    public override string ToString()
    {
        if (From == 0)
            return $"Bar -> {To}";
        if (IsBearOff)
            return $"{From} -> Off";
        return $"{From} -> {To}{(IsHit ? " (hit)" : "")}";
    }
}
