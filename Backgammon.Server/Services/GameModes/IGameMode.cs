namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Defines behavior for different game modes (Multiplayer, Analysis, Tutorial, etc.)
/// </summary>
public interface IGameMode
{
    /// <summary>
    /// Whether this mode should track player statistics
    /// </summary>
    bool ShouldTrackStats { get; }

    /// <summary>
    /// Whether this mode should persist games to the database
    /// </summary>
    bool ShouldPersist { get; }

    /// <summary>
    /// Determines if the given connection has control in the current game state
    /// </summary>
    bool IsPlayerTurn(string connectionId, GameSession session);

    /// <summary>
    /// Get UI features available in this mode
    /// </summary>
    GameModeFeatures GetFeatures();
}
