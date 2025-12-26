using Backgammon.Core;

namespace Backgammon.Web.Models;

/// <summary>
/// Data transfer object representing the current state of a game.
/// This is sent to all clients whenever the game state changes.
/// </summary>
public class GameState
{
    public string GameId { get; set; } = string.Empty;
    public string WhitePlayerId { get; set; } = string.Empty;
    public string RedPlayerId { get; set; } = string.Empty;
    public CheckerColor CurrentPlayer { get; set; }
    public int[] Dice { get; set; } = Array.Empty<int>();
    public int[] RemainingMoves { get; set; } = Array.Empty<int>();
    public List<MoveDto> ValidMoves { get; set; } = new();
    public PointState[] Board { get; set; } = Array.Empty<PointState>();
    public int WhiteCheckersOnBar { get; set; }
    public int RedCheckersOnBar { get; set; }
    public int WhiteBornOff { get; set; }
    public int RedBornOff { get; set; }
    public GameStatus Status { get; set; }
    public CheckerColor? Winner { get; set; }
    public string? WinType { get; set; }
}

/// <summary>
/// Represents a single point on the board
/// </summary>
public class PointState
{
    public int Position { get; set; }
    public CheckerColor? Color { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// Data transfer object for a move
/// </summary>
public class MoveDto
{
    public int From { get; set; }
    public int To { get; set; }
    public int DieValue { get; set; }
    public bool IsHit { get; set; }
}

public enum GameStatus
{
    WaitingForPlayer,
    InProgress,
    Completed
}
