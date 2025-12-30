namespace Backgammon.Server.Configuration;

/// <summary>
/// Feature flags for controlling optional server functionality.
/// Configure these values in appsettings.json under the "Features" section.
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Enable or disable always-running bot games.
    /// </summary>
    public bool BotGamesEnabled { get; set; } = true;

    /// <summary>
    /// Maximum number of concurrent bot games to run (1-2 recommended).
    /// </summary>
    public int MaxBotGames { get; set; } = 2;

    /// <summary>
    /// Delay in seconds before restarting a bot game after completion.
    /// This allows spectators to see the final results.
    /// </summary>
    public int BotGameRestartDelaySeconds { get; set; } = 12;
}
