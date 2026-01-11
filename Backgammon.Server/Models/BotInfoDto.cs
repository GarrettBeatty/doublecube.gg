using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Information about an available AI bot.
/// </summary>
[TranspilationSource]
public class BotInfoDto
{
    /// <summary>
    /// Unique identifier for the bot type.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the bot.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the bot's playing style.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Difficulty level (1-5 stars).
    /// </summary>
    public int Difficulty { get; set; }

    /// <summary>
    /// Whether this bot is currently available to play against.
    /// </summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>
    /// Icon identifier for the bot (e.g., "robot", "brain", "dice").
    /// </summary>
    public string Icon { get; set; } = "robot";
}
