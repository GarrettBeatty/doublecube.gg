using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Grains;

/// <summary>
/// Persistent state for <see cref="MatchGrain"/>, serialized by Orleans via
/// IPersistentState&lt;MatchGrainState&gt;.
///
/// Holds everything required to rehydrate a match grain after deactivation/silo
/// restart. Connection tracking lives on the grain instance (ephemeral) and is
/// not persisted.
/// </summary>
[GenerateSerializer]
public sealed class MatchGrainState
{
    // ===== Identity / config =====

    [Id(0)]
    public string MatchId { get; set; } = string.Empty;

    [Id(1)]
    public int TargetScore { get; set; }

    [Id(2)]
    public string Player1Id { get; set; } = string.Empty;

    [Id(3)]
    public string Player2Id { get; set; } = string.Empty;

    [Id(4)]
    public string Player1Name { get; set; } = string.Empty;

    [Id(5)]
    public string Player2Name { get; set; } = string.Empty;

    [Id(6)]
    public string? Player1DisplayName { get; set; }

    [Id(7)]
    public string? Player2DisplayName { get; set; }

    [Id(8)]
    public string OpponentType { get; set; } = "Friend";

    [Id(9)]
    public bool IsOpenLobby { get; set; }

    [Id(10)]
    public bool IsRated { get; set; } = true;

    // ===== Scoring =====

    [Id(11)]
    public int Player1Score { get; set; }

    [Id(12)]
    public int Player2Score { get; set; }

    [Id(13)]
    public bool IsCrawfordGame { get; set; }

    [Id(14)]
    public bool HasCrawfordGameBeenPlayed { get; set; }

    // ===== Time control =====

    [Id(15)]
    public TimeControlConfig TimeControl { get; set; } = new();

    // ===== Status / lifecycle =====

    [Id(16)]
    public MatchStatus Status { get; set; } = MatchStatus.WaitingForPlayers;

    [Id(17)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Id(18)]
    public DateTime? CompletedAt { get; set; }

    [Id(19)]
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    [Id(20)]
    public string? WinnerId { get; set; }

    [Id(21)]
    public int DurationSeconds { get; set; }

    // ===== Games tracking =====

    [Id(22)]
    public string? CurrentGameId { get; set; }

    [Id(23)]
    public List<MatchGameSummary> GamesSummary { get; set; } = new();

    // ===== Correspondence =====

    [Id(24)]
    public bool IsCorrespondence { get; set; }

    [Id(25)]
    public int TimePerMoveDays { get; set; }

    [Id(26)]
    public DateTime? TurnDeadline { get; set; }

    [Id(27)]
    public string? CurrentTurnPlayerId { get; set; }

    /// <summary>
    /// Whether this state has been initialized via CreateAsync/CreateCorrespondenceAsync
    /// or hydrated from the repository fallback during activation.
    /// </summary>
    public bool IsInitialized => !string.IsNullOrEmpty(MatchId);
}
