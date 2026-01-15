namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Analysis mode where a single player controls both sides for practice and position testing
/// </summary>
public class AnalysisMode : IGameMode
{
    private readonly string _controllingPlayerId;

    public AnalysisMode(string controllingPlayerId)
    {
        _controllingPlayerId = controllingPlayerId;
    }

    public bool ShouldTrackStats => false;

    public bool ShouldPersist => false;

    public bool IsPlayerTurn(string connectionId, GameSession session)
    {
        // In analysis mode, the controlling player can play both sides
        // Verify the connectionId belongs to the controlling player
        return session.GetPlayerColor(connectionId) != null;
    }

    public GameModeFeatures GetFeatures() => new()
    {
        AllowChat = false,
        AllowDouble = false,
        AllowImportExport = true,
        ShowAnalysisBadge = true
    };
}
