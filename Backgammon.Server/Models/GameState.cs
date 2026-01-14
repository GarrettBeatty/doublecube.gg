using Backgammon.Core;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object representing the current state of a game.
/// This is sent to all clients whenever the game state changes.
/// </summary>
[TranspilationSource]
public class GameState
{
    public string GameId { get; set; } = string.Empty;

    public string WhitePlayerId { get; set; } = string.Empty;

    public string RedPlayerId { get; set; } = string.Empty;

    public string WhitePlayerName { get; set; } = string.Empty;

    public string RedPlayerName { get; set; } = string.Empty;

    public int? WhiteRating { get; set; }

    public int? RedRating { get; set; }

    public int? WhiteRatingChange { get; set; }

    public int? RedRatingChange { get; set; }

    public CheckerColor CurrentPlayer { get; set; }

    public CheckerColor? YourColor { get; set; }

    public bool IsYourTurn { get; set; }

    public int[] Dice { get; set; } = Array.Empty<int>();

    public int[] CurrentDice => Dice;

    public int[] RemainingMoves { get; set; } = Array.Empty<int>();

    public int MovesMadeThisTurn { get; set; }

    public List<MoveDto> ValidMoves { get; set; } = new();

    public bool HasValidMoves { get; set; }

    public PointState[] Board { get; set; } = Array.Empty<PointState>();

    public int WhiteCheckersOnBar { get; set; }

    public int RedCheckersOnBar { get; set; }

    public int WhiteBornOff { get; set; }

    public int RedBornOff { get; set; }

    public int WhitePipCount { get; set; }

    public int RedPipCount { get; set; }

    public GameStatus Status { get; set; }

    public CheckerColor? Winner { get; set; }

    public string? WinType { get; set; }

    public int DoublingCubeValue { get; set; }

    public string? DoublingCubeOwner { get; set; }

    public bool CanDouble { get; set; }

    public bool IsAnalysisMode { get; set; }

    public bool IsRated { get; set; }

    // Match information
    public string? MatchId { get; set; }

    public int? TargetScore { get; set; }

    public int? Player1Score { get; set; }

    public int? Player2Score { get; set; }

    public bool? IsCrawfordGame { get; set; }

    // Opening roll information
    public bool IsOpeningRoll { get; set; }

    public int? WhiteOpeningRoll { get; set; }

    public int? RedOpeningRoll { get; set; }

    public bool IsOpeningRollTie { get; set; }

    /// <summary>
    /// Action that will occur if player leaves the game: "Abandon" (no points) or "Forfeit" (opponent gets points)
    /// </summary>
    public string LeaveGameAction { get; set; } = "Abandon";

    public string? WhiteUsername { get; set; }

    public string? RedUsername { get; set; }

    // Time control information
    public string? TimeControlType { get; set; }

    public int? DelaySeconds { get; set; }

    public double? WhiteReserveSeconds { get; set; }

    public double? RedReserveSeconds { get; set; }

    public bool? WhiteIsInDelay { get; set; }

    public bool? RedIsInDelay { get; set; }

    public double? WhiteDelayRemaining { get; set; }

    public double? RedDelayRemaining { get; set; }
}
