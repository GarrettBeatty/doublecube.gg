namespace Backgammon.Server.Grains;

/// <summary>
/// Correspondence-specific match fields needed by <see cref="GameGrain"/> so its
/// per-game state DTO can advertise correspondence-mode + deadline to clients.
/// Returned by <see cref="Interfaces.IMatchGrain.GetCorrespondenceInfoAsync"/>.
/// </summary>
[GenerateSerializer]
public sealed class MatchCorrespondenceInfo
{
    [Id(0)]
    public bool IsCorrespondence { get; set; }

    [Id(1)]
    public int TimePerMoveDays { get; set; }

    [Id(2)]
    public DateTime? TurnDeadline { get; set; }
}
