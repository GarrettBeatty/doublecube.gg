namespace Backgammon.Server.Grains;

/// <summary>
/// Result of <see cref="Interfaces.IMatchGrain.CompleteGameAsync"/>: the post-update
/// match scores, Crawford state, and whether the match itself is now complete.
/// </summary>
[GenerateSerializer]
public sealed class MatchCompletionResult
{
    [Id(0)]
    public int Player1Score { get; set; }

    [Id(1)]
    public int Player2Score { get; set; }

    [Id(2)]
    public bool IsCrawfordGame { get; set; }

    [Id(3)]
    public bool IsMatchComplete { get; set; }

    [Id(4)]
    public string? WinnerId { get; set; }
}
