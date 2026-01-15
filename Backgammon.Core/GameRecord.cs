namespace Backgammon.Core;

/// <summary>
/// Represents a complete backgammon game record parsed from SGF format.
/// Used for game replay and analysis.
/// </summary>
public class GameRecord
{
    /// <summary>
    /// White player's name
    /// </summary>
    public string WhitePlayer { get; set; } = string.Empty;

    /// <summary>
    /// Black/Red player's name
    /// </summary>
    public string BlackPlayer { get; set; } = string.Empty;

    /// <summary>
    /// Match length in points (0 for money game)
    /// </summary>
    public int MatchLength { get; set; }

    /// <summary>
    /// Game number within the match (1-based)
    /// </summary>
    public int GameNumber { get; set; } = 1;

    /// <summary>
    /// White's score at start of this game
    /// </summary>
    public int WhiteScore { get; set; }

    /// <summary>
    /// Black's score at start of this game
    /// </summary>
    public int BlackScore { get; set; }

    /// <summary>
    /// Whether this is a Crawford game
    /// </summary>
    public bool IsCrawford { get; set; }

    /// <summary>
    /// Chronological list of turns in the game
    /// </summary>
    public List<GameTurn> Turns { get; set; } = new();

    /// <summary>
    /// Winning player color (null if game incomplete)
    /// </summary>
    public CheckerColor? Winner { get; set; }

    /// <summary>
    /// Type of win (Normal, Gammon, Backgammon)
    /// </summary>
    public WinType? WinType { get; set; }

    /// <summary>
    /// Final cube value
    /// </summary>
    public int CubeValue { get; set; } = 1;

    /// <summary>
    /// Raw SGF string (for re-export)
    /// </summary>
    public string RawSgf { get; set; } = string.Empty;
}
