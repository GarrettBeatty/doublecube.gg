namespace Backgammon.Plugins.Configuration;

/// <summary>
/// Configuration for a specific bot
/// </summary>
public class BotSettings
{
    /// <summary>
    /// Whether this bot is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Bot-specific options (e.g., difficulty level)
    /// </summary>
    public Dictionary<string, object> Options { get; set; } = new();
}
