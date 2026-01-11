namespace Backgammon.Plugins.Models;

/// <summary>
/// Result of analyzing all possible move sequences for a position
/// </summary>
public class BestMovesAnalysis
{
    /// <summary>
    /// Evaluation of the position before any moves
    /// </summary>
    public PositionEvaluation InitialEvaluation { get; set; } = new();

    /// <summary>
    /// Top 5 move sequences ranked by equity
    /// </summary>
    public List<MoveSequenceEvaluation> TopMoves { get; set; } = new();

    /// <summary>
    /// The best move sequence
    /// </summary>
    public MoveSequenceEvaluation? BestMove => TopMoves.FirstOrDefault();

    /// <summary>
    /// The worst valid move sequence (for comparison)
    /// </summary>
    public MoveSequenceEvaluation? WorstValidMove { get; set; }

    /// <summary>
    /// Total number of valid move sequences explored
    /// </summary>
    public int TotalSequencesExplored { get; set; }
}
