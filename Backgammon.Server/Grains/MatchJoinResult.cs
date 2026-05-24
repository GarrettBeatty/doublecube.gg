namespace Backgammon.Server.Grains;

/// <summary>
/// Result of <see cref="Interfaces.IMatchGrain.JoinAsync"/>.
/// Captures enough match state for the caller to send MatchCreated /
/// OpponentJoinedMatch / CorrespondenceTurnNotification events.
/// </summary>
[GenerateSerializer]
public sealed class MatchJoinResult
{
    [Id(0)]
    public string MatchId { get; set; } = string.Empty;

    [Id(1)]
    public string? CurrentGameId { get; set; }

    [Id(2)]
    public int TargetScore { get; set; }

    [Id(3)]
    public string OpponentType { get; set; } = string.Empty;

    [Id(4)]
    public string Player1Id { get; set; } = string.Empty;

    [Id(5)]
    public string? Player2Id { get; set; }

    [Id(6)]
    public string Player1Name { get; set; } = string.Empty;

    [Id(7)]
    public string Player2Name { get; set; } = string.Empty;

    [Id(8)]
    public bool IsCorrespondence { get; set; }
}
