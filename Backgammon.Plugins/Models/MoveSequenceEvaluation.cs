using Backgammon.Core;

namespace Backgammon.Plugins.Models;

/// <summary>
/// Evaluation of a complete move sequence (using all available dice)
/// </summary>
public class MoveSequenceEvaluation
{
    /// <summary>
    /// The sequence of moves in order (primary ordering)
    /// </summary>
    public List<Move> Moves { get; set; } = new();

    /// <summary>
    /// Alternative move orderings for abbreviated moves.
    /// When gnubg returns abbreviated notation like "24/15", there may be multiple
    /// valid intermediate points. This list contains all possible orderings.
    /// The first element is always the same as <see cref="Moves"/>.
    /// </summary>
    public List<List<Move>> Alternatives { get; set; } = new();

    /// <summary>
    /// Evaluation of the position after all moves are executed
    /// </summary>
    public PositionEvaluation FinalEvaluation { get; set; } = new();

    /// <summary>
    /// Equity gained by making these moves (vs doing nothing)
    /// </summary>
    public double EquityGain { get; set; }

    /// <summary>
    /// Move notation string (e.g., "24/20 13/9")
    /// </summary>
    public string Notation
    {
        get
        {
            return string.Join(" ", Moves.Select(m =>
            {
                if (m.IsBearOff)
                {
                    return $"{m.From}/off";
                }

                if (m.From == 0)
                {
                    return $"bar/{m.To}";
                }

                return $"{m.From}/{m.To}";
            }));
        }
    }

    /// <summary>
    /// Normalized notation with moves sorted for deduplication
    /// (e.g., "13/8 8/3" and "8/3 13/8" both become "8/3 13/8")
    /// </summary>
    public string NormalizedNotation
    {
        get
        {
            var moveStrings = Moves.Select(m =>
            {
                if (m.IsBearOff)
                {
                    return $"{m.From}/off";
                }

                if (m.From == 0)
                {
                    return $"bar/{m.To}";
                }

                return $"{m.From}/{m.To}";
            }).OrderBy(s => s).ToList();

            return string.Join(" ", moveStrings);
        }
    }
}
