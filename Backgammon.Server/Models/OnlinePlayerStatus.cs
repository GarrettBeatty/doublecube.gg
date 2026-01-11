using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Online player status.
/// </summary>
[TranspilationSource]
public enum OnlinePlayerStatus
{
    /// <summary>
    /// Player is online but not in a game.
    /// </summary>
    Available,

    /// <summary>
    /// Player is currently in a game.
    /// </summary>
    InGame,

    /// <summary>
    /// Player is looking for a match.
    /// </summary>
    LookingForMatch
}
