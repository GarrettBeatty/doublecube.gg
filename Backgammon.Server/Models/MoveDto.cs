using Orleans;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for a move.
/// </summary>
[TranspilationSource]
[GenerateSerializer]
public class MoveDto
{
    /// <summary>
    /// Gets or sets the starting point (0 = bar, 1-24 = board points).
    /// </summary>
    [Id(0)]
    public int From { get; set; }

    /// <summary>
    /// Gets or sets the destination point (0 or 25 = bear off, 1-24 = board points).
    /// </summary>
    [Id(1)]
    public int To { get; set; }

    /// <summary>
    /// Gets or sets the die value used for this move (for single moves).
    /// </summary>
    [Id(2)]
    public int DieValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this move hits an opponent's blot.
    /// </summary>
    [Id(3)]
    public bool IsHit { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a combined move using multiple dice.
    /// </summary>
    [Id(4)]
    public bool IsCombinedMove { get; set; }

    /// <summary>
    /// Gets or sets the dice values used for combined moves (e.g., [6, 1] or [3, 3, 3]).
    /// </summary>
    [Id(5)]
    public int[]? DiceUsed { get; set; }

    /// <summary>
    /// Gets or sets the intermediate points for combined moves (e.g., [18] for 24→18→17).
    /// </summary>
    [Id(6)]
    public int[]? IntermediatePoints { get; set; }
}
