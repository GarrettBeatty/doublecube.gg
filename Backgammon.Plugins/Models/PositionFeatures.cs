namespace Backgammon.Plugins.Models;

/// <summary>
/// Features extracted from a backgammon position for evaluation.
/// Captures tactical, strategic, and distributional aspects of the position.
/// </summary>
public class PositionFeatures
{
    // Basic race metrics

    /// <summary>
    /// Total pip count for the player (distance to bear off all checkers)
    /// </summary>
    public int PipCount { get; set; }

    /// <summary>
    /// Pip count difference (player - opponent). Negative = losing race.
    /// </summary>
    public int PipDifference { get; set; }

    // Tactical features

    /// <summary>
    /// Number of blots (unprotected single checkers)
    /// </summary>
    public int BlotCount { get; set; }

    /// <summary>
    /// Weighted blot exposure score (considers danger level of each blot)
    /// </summary>
    public int BlotExposure { get; set; }

    /// <summary>
    /// Number of checkers on the bar
    /// </summary>
    public int CheckersOnBar { get; set; }

    // Strategic features

    /// <summary>
    /// Length of longest prime (consecutive owned points forming a wall)
    /// </summary>
    public int PrimeLength { get; set; }

    /// <summary>
    /// Number of anchors (2+ checkers) in opponent's home board
    /// </summary>
    public int AnchorsInOpponentHome { get; set; }

    /// <summary>
    /// Number of home board points owned (1-6 for White, 19-24 for Red)
    /// </summary>
    public int HomeboardCoverage { get; set; }

    // Distribution and position type

    /// <summary>
    /// Distribution quality score (0-1). Higher = better spread, lower = too stacked.
    /// </summary>
    public double Distribution { get; set; }

    /// <summary>
    /// True if checkers from both players can still interact (contact position)
    /// </summary>
    public bool IsContact { get; set; }

    /// <summary>
    /// True if this is a pure racing position (no more contact possible)
    /// </summary>
    public bool IsRace { get; set; }

    // Advanced metrics

    /// <summary>
    /// Wasted pips from inefficient stacking (e.g., 5 checkers on one point)
    /// </summary>
    public int WastedPips { get; set; }

    /// <summary>
    /// Bearing off efficiency (0-1). Only relevant when in bear-off phase.
    /// </summary>
    public double BearoffEfficiency { get; set; }

    /// <summary>
    /// Number of checkers borne off
    /// </summary>
    public int CheckersBornOff { get; set; }
}
