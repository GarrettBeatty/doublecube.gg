namespace Backgammon.Server.Grains;

/// <summary>
/// Result of <see cref="Interfaces.IMatchGrain.EnsureNextGameAsync"/>: either the
/// game ID the caller should join (whether reused or freshly created) or an error
/// message explaining why no game can be returned. Exactly one of the two is set.
/// </summary>
[GenerateSerializer]
public sealed class EnsureNextGameResult
{
    [Id(0)]
    public string? GameId { get; set; }

    [Id(1)]
    public string? Error { get; set; }
}
