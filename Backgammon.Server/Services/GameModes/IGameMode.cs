namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Defines behavior for different game modes (Multiplayer, Analysis, Tutorial, etc.)
/// </summary>
public interface IGameMode
{
    /// <summary>
    /// Determines if the given connection has control in the current game state
    /// </summary>
    bool IsPlayerTurn(string connectionId, GameSession session);

    /// <summary>
    /// Whether this mode should track player statistics
    /// </summary>
    bool ShouldTrackStats { get; }

    /// <summary>
    /// Get UI features available in this mode
    /// </summary>
    GameModeFeatures GetFeatures();
}

/// <summary>
/// Feature flags for different game modes
/// </summary>
public class GameModeFeatures
{
    public bool AllowChat { get; init; }
    public bool AllowDouble { get; init; }
    public bool AllowImportExport { get; init; }
    public bool ShowAnalysisBadge { get; init; }
}
