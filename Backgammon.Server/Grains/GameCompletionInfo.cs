using Backgammon.Core;

namespace Backgammon.Server.Grains;

/// <summary>
/// Input for <see cref="Interfaces.IMatchGrain.CompleteGameAsync"/>.
/// Replaces the non-serializable <see cref="GameResult"/> at the grain boundary;
/// callers (mostly <see cref="GameGrain"/>) build this from their local engine state.
/// </summary>
[GenerateSerializer]
public sealed class GameCompletionInfo
{
    [Id(0)]
    public string WinnerId { get; set; } = string.Empty;

    [Id(1)]
    public CheckerColor? WinnerColor { get; set; }

    [Id(2)]
    public int PointsWon { get; set; }

    [Id(3)]
    public WinType WinType { get; set; }

    [Id(4)]
    public bool IsAbandoned { get; set; }

    [Id(5)]
    public bool IsForfeit { get; set; }
}
