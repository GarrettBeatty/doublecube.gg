namespace Backgammon.Core;

/// <summary>
/// Represents a dice roll in the game
/// </summary>
public class Dice
{
    private readonly Random _random = new();

    public int Die1 { get; private set; }

    public int Die2 { get; private set; }

    /// <summary>
    /// Whether this is a doubles roll
    /// </summary>
    public bool IsDoubles => Die1 == Die2;

    /// <summary>
    /// Get all available moves from this roll
    /// </summary>
    public List<int> GetMoves()
    {
        if (IsDoubles)
        {
            return new List<int> { Die1, Die1, Die1, Die1 };
        }

        return new List<int> { Die1, Die2 };
    }

    /// <summary>
    /// Roll two dice
    /// </summary>
    public void Roll()
    {
        Die1 = _random.Next(1, 7);
        Die2 = _random.Next(1, 7);
    }

    /// <summary>
    /// Roll a single die (for starting the game)
    /// </summary>
    public int RollSingle()
    {
        return _random.Next(1, 7);
    }

    /// <summary>
    /// Set dice values (for testing or opening roll)
    /// </summary>
    public void SetDice(int die1, int die2)
    {
        Die1 = die1;
        Die2 = die2;
    }

    public override string ToString()
    {
        return $"[{Die1}] [{Die2}]";
    }
}
