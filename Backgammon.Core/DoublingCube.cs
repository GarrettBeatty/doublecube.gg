namespace Backgammon.Core;

/// <summary>
/// Represents the doubling cube in backgammon
/// </summary>
public class DoublingCube
{
    /// <summary>
    /// Maximum allowed cube value (standard backgammon limit)
    /// </summary>
    public const int MaxCubeValue = 64;

    public DoublingCube()
    {
        Value = 1;
        Owner = null; // Cube is in the middle
    }

    public int Value { get; private set; }

    public CheckerColor? Owner { get; private set; }

    /// <summary>
    /// Whether the specified player can offer a double
    /// </summary>
    public bool CanDouble(CheckerColor color)
    {
        // Cannot double if already at max value
        if (Value >= MaxCubeValue)
        {
            return false;
        }

        return Owner == null || Owner == color;
    }

    /// <summary>
    /// Double the stakes
    /// </summary>
    /// <returns>True if doubled successfully, false if already at max value</returns>
    public bool Double(CheckerColor newOwner)
    {
        if (Value >= MaxCubeValue)
        {
            return false;
        }

        Value *= 2;
        Owner = newOwner;
        return true;
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
