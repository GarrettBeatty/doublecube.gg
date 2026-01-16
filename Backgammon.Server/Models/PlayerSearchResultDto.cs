using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Player search result data transfer object
/// </summary>
[TranspilationSource]
public class PlayerSearchResultDto
{
    /// <summary>
    /// User's unique ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// User's ELO rating
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Whether the user is currently online
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Total number of games played
    /// </summary>
    public int TotalGames { get; set; }
}
