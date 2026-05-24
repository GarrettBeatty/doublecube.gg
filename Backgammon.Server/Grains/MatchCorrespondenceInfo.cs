namespace Backgammon.Server.Grains;

/// <summary>
/// Match-level fields a <see cref="GameGrain"/> needs to mirror locally so it can
/// build state DTOs and AI MatchContext without doing a grain hop on every read.
/// Fetched from <see cref="Interfaces.IMatchGrain.GetCorrespondenceInfoAsync"/>
/// during activation; values are stable for the lifetime of one game (the next
/// game in the match activates its own GameGrain and re-fetches).
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

    [Id(3)]
    public int TargetScore { get; set; }

    [Id(4)]
    public int Player1Score { get; set; }

    [Id(5)]
    public int Player2Score { get; set; }
}
