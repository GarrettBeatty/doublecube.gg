using Backgammon.Core;

namespace Backgammon.Server.Grains;

/// <summary>
/// Parameters for creating a regular (real-time) match via
/// <see cref="Interfaces.IMatchGrain.CreateMatchAsync"/>.
/// </summary>
[GenerateSerializer]
public sealed class MatchCreationRequest
{
    [Id(0)]
    public string Player1Id { get; set; } = string.Empty;

    [Id(1)]
    public int TargetScore { get; set; }

    [Id(2)]
    public string OpponentType { get; set; } = "Friend";

    [Id(3)]
    public string? Player1DisplayName { get; set; }

    [Id(4)]
    public string? Player2Id { get; set; }

    [Id(5)]
    public TimeControlConfig? TimeControl { get; set; }

    [Id(6)]
    public bool IsRated { get; set; } = true;

    [Id(7)]
    public string AiType { get; set; } = "greedy";
}
