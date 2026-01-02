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

    public bool IsPlayerTurn(string connectionId, GameSession session)
    {
        // Player controls both sides in analysis mode
        return session.WhitePlayerId == _controllingPlayerId ||
               session.RedPlayerId == _controllingPlayerId;
    }

    public GameModeFeatures GetFeatures() => new()
    {
        AllowChat = false,
        AllowDouble = false,
        AllowImportExport = true,
        ShowAnalysisBadge = true
    };
}
