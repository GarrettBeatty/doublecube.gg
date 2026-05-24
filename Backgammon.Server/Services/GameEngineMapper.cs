using Backgammon.Core;

namespace Backgammon.Server.Services;

/// <summary>
/// Utility helpers for game engine operations.
/// </summary>
public static class GameEngineMapper
{
    /// <summary>
    /// Check if a player ID represents an AI opponent
    /// </summary>
    public static bool IsAiPlayer(string? playerId)
    {
        return playerId?.StartsWith("ai_") == true;
    }

    /// <summary>
    /// Check if a player ID looks like a registered user ID (GUID format)
    /// </summary>
    public static bool IsRegisteredUserId(string? playerId)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            return false;
        }

        return Guid.TryParse(playerId, out _);
    }
}
