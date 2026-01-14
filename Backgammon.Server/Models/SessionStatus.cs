namespace Backgammon.Server.Models;

/// <summary>
/// Represents the lifecycle state of a GameSession in memory.
/// Distinct from Game.Status (persistence layer) and Match.Status (match lifecycle).
/// </summary>
public enum SessionStatus
{
    /// <summary>
    /// Session created, waiting for second player to join
    /// </summary>
    WaitingForOpponent,

    /// <summary>
    /// Both players joined, game is actively being played
    /// </summary>
    InProgress,

    /// <summary>
    /// Game completed (natural win, forfeit, timeout, etc.)
    /// </summary>
    Completed
}
