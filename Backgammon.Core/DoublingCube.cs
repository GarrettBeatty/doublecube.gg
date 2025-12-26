namespace Backgammon.Core;

/// <summary>
/// Represents the doubling cube in backgammon
/// </summary>
public class DoublingCube
{
    public int Value { get; private set; }
    public CheckerColor? Owner { get; private set; }

    public DoublingCube()
    {
        Value = 1;
        Owner = null; // Cube is in the middle
    }

    /// <summary>
    /// Whether the specified player can offer a double
    /// </summary>
    public bool CanDouble(CheckerColor color)
    {
        return Owner == null || Owner == color;
    }

    /// <summary>
    /// Double the stakes
    /// </summary>
    public void Double(CheckerColor newOwner)
    {
        Value *= 2;
        Owner = newOwner;
    }

    /// <summary>
    /// Reset for a new game
    /// </summary>
    public void Reset()
    {
        Value = 1;
        Owner = null;
    }
}
