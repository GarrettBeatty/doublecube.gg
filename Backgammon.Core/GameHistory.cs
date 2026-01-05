namespace Backgammon.Core;

/// <summary>
/// Complete history of a backgammon game, tracking all turns for analysis and replay
/// </summary>
public class GameHistory
{
    /// <summary>
    /// Chronological list of all turns played in the game
    /// </summary>
    public List<TurnSnapshot> Turns { get; set; } = new();

    /// <summary>
    /// Total number of turns played
    /// </summary>
    public int TurnCount => Turns.Count;

    /// <summary>
    /// Get a specific turn by number (1-based)
    /// </summary>
    public TurnSnapshot? GetTurn(int turnNumber)
    {
        return Turns.FirstOrDefault(t => t.TurnNumber == turnNumber);
    }

    /// <summary>
    /// Get all turns for a specific player
    /// </summary>
    public List<TurnSnapshot> GetPlayerTurns(CheckerColor player)
    {
        return Turns.Where(t => t.Player == player).ToList();
    }

    /// <summary>
    /// Clear all history
    /// </summary>
    public void Clear()
    {
        Turns.Clear();
    }
}
