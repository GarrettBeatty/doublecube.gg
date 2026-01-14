namespace Backgammon.Server.Services;

/// <summary>
/// Manages AI players for matches, ensuring consistency across game continuations.
/// Provides single source of truth for AI player IDs and names in multi-game matches.
/// </summary>
public interface IAiPlayerManager
{
    /// <summary>
    /// Gets or creates an AI player for the specified match.
    /// Returns the same AI player ID across all games in the match for consistency.
    /// </summary>
    /// <param name="matchId">The match ID</param>
    /// <param name="aiType">The AI difficulty/type (e.g., "Random", "Greedy")</param>
    /// <returns>The AI player ID for this match</returns>
    string GetOrCreateAiForMatch(string matchId, string aiType = "Greedy");

    /// <summary>
    /// Gets the display name for an AI player based on match configuration.
    /// </summary>
    /// <param name="matchId">The match ID</param>
    /// <param name="aiType">The AI difficulty/type</param>
    /// <returns>Display name for the AI (e.g., "Greedy Bot", "Random Bot")</returns>
    string GetAiNameForMatch(string matchId, string aiType);

    /// <summary>
    /// Removes AI player mapping when match completes to free memory.
    /// </summary>
    /// <param name="matchId">The completed match ID</param>
    void RemoveMatch(string matchId);
}
