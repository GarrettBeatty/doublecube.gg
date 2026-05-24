namespace Backgammon.Server.Grains;

/// <summary>
/// Parameters for creating a correspondence (async) match via
/// <see cref="Interfaces.IMatchGrain.CreateCorrespondenceMatchAsync"/>.
/// </summary>
[GenerateSerializer]
public sealed class CorrespondenceMatchCreationRequest
{
    [Id(0)]
    public string Player1Id { get; set; } = string.Empty;

    [Id(1)]
    public int TargetScore { get; set; }

    [Id(2)]
    public int TimePerMoveDays { get; set; }

    [Id(3)]
    public string OpponentType { get; set; } = "Friend";

    [Id(4)]
    public string? Player1DisplayName { get; set; }

    [Id(5)]
    public string? Player2Id { get; set; }

    [Id(6)]
    public bool IsRated { get; set; } = true;
}
