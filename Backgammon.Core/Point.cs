namespace Backgammon.Core;

/// <summary>
/// Represents a point (triangle) on the backgammon board
/// </summary>
public class Point
{
    public Point(int position)
    {
        Position = position;
        Checkers = new List<CheckerColor>();
    }

    public int Position { get; }

    public List<CheckerColor> Checkers { get; }

    /// <summary>
    /// The color of checkers on this point (null if empty)
    /// </summary>
    public CheckerColor? Color => Checkers.Count > 0 ? Checkers[0] : null;

    /// <summary>
    /// Number of checkers on this point
    /// </summary>
    public int Count => Checkers.Count;

    /// <summary>
    /// Whether this point is a blot (single checker)
    /// </summary>
    public bool IsBlot => Count == 1;

    /// <summary>
    /// Whether an opposing checker can land on this point
    /// </summary>
    public bool IsOpen(CheckerColor color)
    {
        return Color == null || Color == color || Count < 2;
    }

    /// <summary>
    /// Add a checker to this point
    /// </summary>
    public void AddChecker(CheckerColor color)
    {
        if (Checkers.Count > 0 && Checkers[0] != color)
        {
            throw new InvalidOperationException("Cannot add different color to point");
        }

        Checkers.Add(color);
    }

    /// <summary>
    /// Remove a checker from this point
    /// </summary>
    public CheckerColor RemoveChecker()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("No checkers to remove");
        }

        var color = Checkers[^1];
        Checkers.RemoveAt(Checkers.Count - 1);
        return color;
    }
}
