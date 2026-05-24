namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Analysis mode where a single player controls both sides for practice and position testing.
/// </summary>
public class AnalysisMode : IGameMode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AnalysisMode"/> class.
    /// </summary>
    /// <param name="controllingPlayerId">The ID of the player who controls both sides.</param>
    public AnalysisMode(string controllingPlayerId)
    {
        ControllingPlayerId = controllingPlayerId;
    }

    /// <summary>
    /// Gets the ID of the player who controls both sides in this analysis session.
    /// </summary>
    public string ControllingPlayerId { get; }

    /// <inheritdoc/>
    public bool ShouldTrackStats => false;

    /// <inheritdoc/>
    public bool ShouldPersist => false;

    /// <inheritdoc/>
    public GameModeFeatures GetFeatures() => new()
    {
        AllowChat = false,
        AllowDouble = false,
        AllowImportExport = true,
        ShowAnalysisBadge = true
    };
}
