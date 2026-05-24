using Backgammon.Core;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;

namespace Backgammon.Server.Grains;

/// <summary>
/// Persistent state for <see cref="GameGrain"/>, serialized by Orleans via
/// IPersistentState&lt;GameGrainState&gt;.
///
/// Holds everything required to rehydrate a game grain after deactivation/silo restart.
/// Ephemeral state (connection IDs, the live <see cref="GameEngine"/> reference, the
/// reactivation timer) lives on the grain itself, not here.
/// </summary>
[GenerateSerializer]
public sealed class GameGrainState
{
    // ===== Player identity / display =====

    [Id(0)]
    public string? WhitePlayerId { get; set; }

    [Id(1)]
    public string? RedPlayerId { get; set; }

    [Id(2)]
    public string? WhitePlayerName { get; set; }

    [Id(3)]
    public string? RedPlayerName { get; set; }

    [Id(4)]
    public int? WhiteRating { get; set; }

    [Id(5)]
    public int? RedRating { get; set; }

    [Id(6)]
    public int? WhiteRatingBefore { get; set; }

    [Id(7)]
    public int? RedRatingBefore { get; set; }

    // ===== Session lifecycle =====

    [Id(8)]
    public SessionStatus Status { get; set; } = SessionStatus.WaitingForOpponent;

    [Id(9)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Id(10)]
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    [Id(11)]
    public bool IsRated { get; set; } = true;

    [Id(12)]
    public bool IsBotGame { get; set; }

    // ===== Match metadata =====

    [Id(13)]
    public string? MatchId { get; set; }

    [Id(14)]
    public int? TargetScore { get; set; }

    [Id(15)]
    public int? Player1Score { get; set; }

    [Id(16)]
    public int? Player2Score { get; set; }

    [Id(17)]
    public bool? IsCrawfordGame { get; set; }

    // ===== Time control =====

    [Id(18)]
    public TimeControlConfig? TimeControl { get; set; }

    [Id(19)]
    public bool IsCorrespondence { get; set; }

    [Id(20)]
    public int? TimePerMoveDays { get; set; }

    [Id(21)]
    public DateTime? TurnDeadline { get; set; }

    // ===== Doubling cube offer state =====

    [Id(22)]
    public bool HasPendingDoubleOffer { get; set; }

    [Id(23)]
    public string? PendingDoubleOfferedBy { get; set; }

    // ===== GameEngine snapshot =====
    // The engine itself isn't Orleans-serializable (private setters in Backgammon.Core),
    // so we capture the data needed to reconstruct it as primitives. RestoreEngine in
    // GameGrain rebuilds a live engine from these fields on activation.

    [Id(24)]
    public List<PointStateDto> BoardState { get; set; } = new();

    [Id(25)]
    public int WhiteCheckersOnBar { get; set; }

    [Id(26)]
    public int RedCheckersOnBar { get; set; }

    [Id(27)]
    public int WhiteBornOff { get; set; }

    [Id(28)]
    public int RedBornOff { get; set; }

    [Id(29)]
    public string CurrentPlayer { get; set; } = "White";

    [Id(30)]
    public int Die1 { get; set; }

    [Id(31)]
    public int Die2 { get; set; }

    [Id(32)]
    public List<int> RemainingMoves { get; set; } = new();

    [Id(33)]
    public int DoublingCubeValue { get; set; } = 1;

    [Id(34)]
    public string? DoublingCubeOwner { get; set; }

    [Id(35)]
    public bool GameStarted { get; set; }

    [Id(36)]
    public string? Winner { get; set; }

    [Id(37)]
    public string GameSgf { get; set; } = string.Empty;
}
