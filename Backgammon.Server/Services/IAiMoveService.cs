namespace Backgammon.Server.Services;

/// <summary>
/// Service for handling AI opponent moves in backgammon games.
/// </summary>
public interface IAiMoveService
{
    /// <summary>
    /// Executes an AI turn including rolling dice, making moves, and ending the turn.
    /// Includes realistic delays between actions for better UX.
    /// </summary>
    /// <param name="session">The game session where AI should play</param>
    /// <param name="aiPlayerId">The AI player's ID</param>
    /// <param name="broadcastUpdate">Callback to broadcast game state updates to clients</param>
    /// <returns>Task that completes when the AI turn is finished</returns>
    Task ExecuteAiTurnAsync(GameSession session, string aiPlayerId, Func<Task> broadcastUpdate);

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
