namespace Backgammon.Analysis.Gnubg;

/// <summary>
/// Represents a single move analysis result from gnubg
/// </summary>
public class MoveAnalysis
{
    public int Rank { get; set; }

    public string Notation { get; set; } = string.Empty;

    public double Equity { get; set; }
}
