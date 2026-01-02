namespace Backgammon.Core;

/// <summary>
/// Pure game outcome data - represents the result of a completed or in-progress game
/// This is the domain object with no server/infrastructure concerns
/// </summary>
public class Game
{
    public Game()
    {
        Status = GameStatus.InProgress;
    }

    public Game(string gameId)
        : this()
    {
        GameId = gameId;
    }

    /// <summary>
    /// Unique game identifier
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// Winner of the game (null if abandoned or in progress)
    /// </summary>
    public CheckerColor? Winner { get; set; }

    /// <summary>
    /// Points won (1 for normal, 2 for gammon, 3 for backgammon)
    /// </summary>
    public int Stakes { get; set; }

    /// <summary>
    /// Type of win
    /// </summary>
    public WinType WinType { get; set; }

    /// <summary>
    /// Match ID if this game is part of a match
    /// </summary>
    public string? MatchId { get; set; }

    /// <summary>
    /// Whether this game is part of a match
    /// </summary>
    public bool IsMatchGame { get; set; }

    /// <summary>
    /// Whether this is a Crawford game (no doubling allowed)
    /// </summary>
    public bool IsCrawfordGame { get; set; }

    /// <summary>
    /// Complete move history for the game
    /// </summary>
    public List<Move> MoveHistory { get; set; } = new();

    /// <summary>
    /// Current status of the game
    /// </summary>
    public GameStatus Status { get; set; }
}
