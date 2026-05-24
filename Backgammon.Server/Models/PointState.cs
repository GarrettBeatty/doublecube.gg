using Orleans;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Represents a single point on the board
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class PointState
{
    [Id(0)]
    public int Position { get; set; }

    [Id(1)]
    public Backgammon.Core.CheckerColor? Color { get; set; }

    [Id(2)]
    public int Count { get; set; }
}
