namespace Backgammon.Server.Services.GameModes;

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
