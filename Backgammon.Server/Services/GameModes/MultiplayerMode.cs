namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Standard multiplayer mode where two different players compete.
/// </summary>
public class MultiplayerMode : IGameMode
{
    /// <inheritdoc/>
    public bool ShouldTrackStats => true;

    /// <inheritdoc/>
    public bool ShouldPersist => true;

    /// <inheritdoc/>
    public GameModeFeatures GetFeatures() => new()
    {
        AllowChat = true,
        AllowDouble = true,
        AllowImportExport = false,
        ShowAnalysisBadge = false
    };
}
