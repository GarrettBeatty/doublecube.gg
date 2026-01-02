namespace Backgammon.Core;

/// <summary>
/// Represents a player in the game
/// </summary>
public class Player
{
    public Player(CheckerColor color, string name)
    {
        Color = color;
        Name = name;
        CheckersOnBar = 0;
        CheckersBornOff = 0;
    }

    public CheckerColor Color { get; }

    public string Name { get; }

    public int CheckersOnBar { get; set; }

    public int CheckersBornOff { get; set; }

    /// <summary>
    /// Get the home board range for this player (1-6 or 19-24)
    /// </summary>
    public (int Start, int End) GetHomeBoardRange()
    {
        return Color == CheckerColor.White ? (1, 6) : (19, 24);
    }

    /// <summary>
    /// Get the direction of movement for this player
    /// White moves from 24->1, Red moves from 1->24
    /// </summary>
    public int GetDirection()
    {
        return Color == CheckerColor.White ? -1 : 1;
    }
}
