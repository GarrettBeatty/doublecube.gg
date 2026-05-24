namespace Backgammon.Server.Grains;

/// <summary>
/// Result of <see cref="Interfaces.IMatchGrain.CreateMatchAsync"/> /
/// <see cref="Interfaces.IMatchGrain.CreateCorrespondenceMatchAsync"/>.
/// Captures the data callers need to assemble client notifications
/// without re-reading the repository.
/// </summary>
[GenerateSerializer]
public sealed class MatchCreationResult
{
    [Id(0)]
    public string MatchId { get; set; } = string.Empty;

    [Id(1)]
    public string FirstGameId { get; set; } = string.Empty;

    [Id(2)]
    public string Player1Id { get; set; } = string.Empty;

    [Id(3)]
    public string? Player2Id { get; set; }

    [Id(4)]
    public string Player1Name { get; set; } = string.Empty;

    [Id(5)]
    public string? Player2Name { get; set; }

    [Id(6)]
    public int TargetScore { get; set; }

    [Id(7)]
    public string OpponentType { get; set; } = string.Empty;

    [Id(8)]
    public bool IsRated { get; set; }

    [Id(9)]
    public bool IsCorrespondence { get; set; }

    [Id(10)]
    public int TimePerMoveDays { get; set; }

    [Id(11)]
    public DateTime? TurnDeadline { get; set; }
}
