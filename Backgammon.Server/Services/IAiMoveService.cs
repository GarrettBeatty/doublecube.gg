namespace Backgammon.Server.Services;

/// <summary>
/// Service for handling AI opponent moves in backgammon games.
/// </summary>
public interface IAiMoveService
{
    /// <summary>
    /// Executes an AI turn including rolling dice, making moves, and ending the turn.
    /// Includes realistic delays between actions for better UX.
    /// If the AI offers a double, the turn pauses and returns true to indicate
    /// the human must respond before the turn can continue.
    /// </summary>
    /// <param name="session">The game session where AI should play</param>
    /// <param name="aiPlayerId">The AI player's ID</param>
    /// <param name="broadcastUpdate">Callback to broadcast game state updates to clients</param>
    /// <param name="notifyDoubleOffered">Callback to notify opponent of double offer</param>
    /// <returns>True if AI offered a double and is waiting for response, false if turn completed normally</returns>
    Task<bool> ExecuteAiTurnAsync(
        GameSession session,
        string aiPlayerId,
        Func<Task> broadcastUpdate,
        Func<int, int, Task>? notifyDoubleOffered = null);

    /// <summary>
    /// Checks if a player ID represents an AI opponent.
    /// </summary>
    /// <param name="playerId">The player ID to check</param>
    /// <returns>True if the player is an AI, false otherwise</returns>
    bool IsAiPlayer(string? playerId);

    /// <summary>
    /// Generates a new AI player ID.
    /// </summary>
    /// <param name="aiType">The type of AI: "greedy" or "random" (default: "greedy")</param>
    /// <returns>A unique AI player ID</returns>
    string GenerateAiPlayerId(string aiType = "greedy");
}
