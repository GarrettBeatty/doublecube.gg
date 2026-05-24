namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Defines behavior for different game modes (Multiplayer, Analysis, Tutorial, etc.)
/// </summary>
public interface IGameMode
{
    /// <summary>
    /// Whether this mode should track player statistics.
    /// </summary>
    bool ShouldTrackStats { get; }

    /// <summary>
    /// Whether this mode should persist games to the database.
    /// </summary>
    bool ShouldPersist { get; }

    /// <summary>
    /// Get UI features available in this mode.
    /// </summary>
    GameModeFeatures GetFeatures();
}
