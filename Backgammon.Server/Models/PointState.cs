namespace Backgammon.Server.Models;

/// <summary>
/// Represents a single point on the board
/// </summary>
public class PointState
{
    public int Position { get; set; }

    public Backgammon.Core.CheckerColor? Color { get; set; }

    public int Count { get; set; }
}
