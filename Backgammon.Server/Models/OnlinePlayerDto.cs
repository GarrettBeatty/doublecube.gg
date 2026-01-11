using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Online player information for the Players page.
/// </summary>
[TranspilationSource]
public class OnlinePlayerDto
{
    /// <summary>
    /// User's unique ID.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown to other players.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Current ELO rating.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Current status of the player.
    /// </summary>
    public OnlinePlayerStatus Status { get; set; }

    /// <summary>
    /// Game ID if player is currently in a game.
    /// </summary>
    public string? CurrentGameId { get; set; }

    /// <summary>
    /// Whether the viewing user is friends with this player.
    /// </summary>
    public bool IsFriend { get; set; }
}
