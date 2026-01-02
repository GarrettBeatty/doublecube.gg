using Backgammon.Core;

namespace Backgammon.AI;

/// <summary>
/// Result of a single game
/// </summary>
public class GameResult
{
    public CheckerColor Winner { get; set; }

    public int Points { get; set; }

    public int Turns { get; set; }

    public int WhiteBornOff { get; set; }

    public int RedBornOff { get; set; }
}
