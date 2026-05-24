using Backgammon.Core;
using Orleans;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object representing the current state of a game.
/// This is sent to all clients whenever the game state changes.
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class GameState
{
    [Id(0)]
    public string GameId { get; set; } = string.Empty;

    [Id(1)]
    public string WhitePlayerId { get; set; } = string.Empty;

    [Id(2)]
    public string RedPlayerId { get; set; } = string.Empty;

    [Id(3)]
    public string WhitePlayerName { get; set; } = string.Empty;

    [Id(4)]
    public string RedPlayerName { get; set; } = string.Empty;

    [Id(5)]
    public int? WhiteRating { get; set; }

    [Id(6)]
    public int? RedRating { get; set; }

    [Id(7)]
    public int? WhiteRatingChange { get; set; }

    [Id(8)]
    public int? RedRatingChange { get; set; }

    [Id(9)]
    public CheckerColor CurrentPlayer { get; set; }

    [Id(10)]
    public CheckerColor? YourColor { get; set; }

    [Id(11)]
    public bool IsYourTurn { get; set; }

    [Id(12)]
    public int[] Dice { get; set; } = Array.Empty<int>();

    public int[] CurrentDice => Dice;

    [Id(13)]
    public int[] RemainingMoves { get; set; } = Array.Empty<int>();

    [Id(14)]
    public int MovesMadeThisTurn { get; set; }

    /// <summary>
    /// History of all completed turns in this game
    /// </summary>
    [Id(15)]
    public List<TurnSnapshotDto> TurnHistory { get; set; } = new();

    /// <summary>
    /// Moves made so far in the current turn (before EndTurn)
    /// </summary>
    [Id(16)]
    public List<string> CurrentTurnMoves { get; set; } = new();

    [Id(17)]
    public List<MoveDto> ValidMoves { get; set; } = new();

    [Id(18)]
    public bool HasValidMoves { get; set; }

    [Id(19)]
    public PointState[] Board { get; set; } = Array.Empty<PointState>();

    [Id(20)]
    public int WhiteCheckersOnBar { get; set; }

    [Id(21)]
    public int RedCheckersOnBar { get; set; }

    [Id(22)]
    public int WhiteBornOff { get; set; }

    [Id(23)]
    public int RedBornOff { get; set; }

    [Id(24)]
    public int WhitePipCount { get; set; }

    [Id(25)]
    public int RedPipCount { get; set; }

    [Id(26)]
    public GameStatus Status { get; set; }

    [Id(27)]
    public CheckerColor? Winner { get; set; }

    [Id(28)]
    public string? WinType { get; set; }

    [Id(29)]
    public int DoublingCubeValue { get; set; }

    [Id(30)]
    public string? DoublingCubeOwner { get; set; }

    [Id(31)]
    public bool CanDouble { get; set; }

    /// <summary>
    /// Indicates if there is a pending double offer waiting for response.
    /// </summary>
    [Id(32)]
    public bool HasPendingDoubleOffer { get; set; }

    /// <summary>
    /// True if this player offered a double and is waiting for opponent's response.
    /// </summary>
    [Id(33)]
    public bool IsAwaitingDoubleResponse { get; set; }

    /// <summary>
    /// True if opponent offered a double and this player needs to respond.
    /// </summary>
    [Id(34)]
    public bool HasReceivedDoubleOffer { get; set; }

    /// <summary>
    /// What the new cube value would be if the pending double is accepted.
    /// </summary>
    [Id(35)]
    public int PendingDoubleNewValue { get; set; }

    [Id(36)]
    public bool IsAnalysisMode { get; set; }

    [Id(37)]
    public bool IsRated { get; set; }

    // Match information
    [Id(38)]
    public string? MatchId { get; set; }

    [Id(39)]
    public int? TargetScore { get; set; }

    [Id(40)]
    public int? Player1Score { get; set; }

    [Id(41)]
    public int? Player2Score { get; set; }

    [Id(42)]
    public bool? IsCrawfordGame { get; set; }

    // Opening roll information
    [Id(43)]
    public bool IsOpeningRoll { get; set; }

    [Id(44)]
    public int? WhiteOpeningRoll { get; set; }

    [Id(45)]
    public int? RedOpeningRoll { get; set; }

    [Id(46)]
    public bool IsOpeningRollTie { get; set; }

    /// <summary>
    /// Action that will occur if player leaves the game: "Abandon" (no points) or "Forfeit" (opponent gets points)
    /// </summary>
    [Id(47)]
    public string LeaveGameAction { get; set; } = "Abandon";

    [Id(48)]
    public string? WhiteUsername { get; set; }

    [Id(49)]
    public string? RedUsername { get; set; }

    // Time control information
    [Id(50)]
    public string? TimeControlType { get; set; }

    [Id(51)]
    public int? DelaySeconds { get; set; }

    [Id(52)]
    public double? WhiteReserveSeconds { get; set; }

    [Id(53)]
    public double? RedReserveSeconds { get; set; }

    [Id(54)]
    public bool? WhiteIsInDelay { get; set; }

    [Id(55)]
    public bool? RedIsInDelay { get; set; }

    [Id(56)]
    public double? WhiteDelayRemaining { get; set; }

    [Id(57)]
    public double? RedDelayRemaining { get; set; }

    // Correspondence game information
    [Id(58)]
    public bool IsCorrespondence { get; set; }

    [Id(59)]
    public int? TimePerMoveDays { get; set; }

    [Id(60)]
    public DateTime? TurnDeadline { get; set; }
}
