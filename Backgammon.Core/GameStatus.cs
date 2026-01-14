namespace Backgammon.Core;

/// <summary>
/// Game status enumeration
/// </summary>
public enum GameStatus
{
    InProgress,
    Completed,    // Natural completion (player bore off all checkers)
    Abandoned,    // Game never started (no rolls, or opponent never joined) - NO points awarded
    Forfeit // Player quit mid-game (opponent wins points based on board state)
}
