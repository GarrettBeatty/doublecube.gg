namespace Backgammon.Core;

/// <summary>
/// Represents a checker move (single or combined using multiple dice).
/// </summary>
public class Move
{
    /// <summary>
    /// Creates a single-die move.
    /// </summary>
    public Move(int from, int to, int dieValue, bool isHit = false)
    {
        From = from;
        To = to;
        DieValue = dieValue;
        IsHit = isHit;
    }

    /// <summary>
    /// Creates a combined move using multiple dice.
    /// </summary>
    /// <param name="from">Starting point.</param>
    /// <param name="to">Final destination.</param>
    /// <param name="diceUsed">Individual die values used (e.g., [6, 1]).</param>
    /// <param name="intermediatePoints">Intermediate landing points (excluding final destination).</param>
    /// <param name="isHit">Whether the final destination hits an opponent's blot.</param>
    public Move(int from, int to, int[] diceUsed, int[]? intermediatePoints, bool isHit = false)
    {
        From = from;
        To = to;
        DiceUsed = diceUsed;
        IntermediatePoints = intermediatePoints;
        DieValue = diceUsed.Sum();
        IsHit = isHit;
    }

    /// <summary>
    /// Starting point (0 = bar, 1-24 = board points).
    /// </summary>
    public int From { get; set; }

    /// <summary>
    /// Destination point (0 or 25 = bear off, 1-24 = board points).
    /// </summary>
    public int To { get; set; }

    /// <summary>
    /// The die value used for this move. For combined moves, this is the sum of all dice used.
    /// </summary>
    public int DieValue { get; set; }

    /// <summary>
    /// Whether this move hits an opponent's blot (at final destination for combined moves).
    /// </summary>
    public bool IsHit { get; set; }

    /// <summary>
    /// Opponent's checkers on bar before this move (for undo).
    /// </summary>
    public int OpponentCheckersOnBarBefore { get; set; }

    /// <summary>
    /// Current player's checkers born off before this move (for undo).
    /// </summary>
    public int CurrentPlayerBornOffBefore { get; set; }

    /// <summary>
    /// Individual die values used for combined moves (e.g., [6, 1] or [3, 3, 3]).
    /// Null for single-die moves.
    /// </summary>
    public int[]? DiceUsed { get; set; }

    /// <summary>
    /// Intermediate landing points for combined moves (excludes final destination).
    /// For example, [18] for a path 24 -> 18 -> 17.
    /// Null for single-die moves.
    /// </summary>
    public int[]? IntermediatePoints { get; set; }

    /// <summary>
    /// Whether this is a combined move using multiple dice.
    /// </summary>
    public bool IsCombined => DiceUsed != null && DiceUsed.Length > 1;

    /// <summary>
    /// Whether this is a bear-off move.
    /// </summary>
    public bool IsBearOff => To == 0 || To == 25;

    public override string ToString()
    {
        var hitSuffix = IsHit ? " (hit)" : string.Empty;
        var combinedSuffix = IsCombined ? $" [dice: {string.Join("+", DiceUsed!)}]" : string.Empty;

        if (From == 0)
        {
            return $"Bar -> {To}{hitSuffix}{combinedSuffix}";
        }

        if (IsBearOff)
        {
            return $"{From} -> Off{combinedSuffix}";
        }

        return $"{From} -> {To}{hitSuffix}{combinedSuffix}";
    }
}
