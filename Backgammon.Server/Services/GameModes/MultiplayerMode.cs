namespace Backgammon.Server.Services.GameModes;

/// <summary>
/// Standard multiplayer mode where two different players compete
/// </summary>
public class MultiplayerMode : IGameMode
{
    public bool ShouldTrackStats => true;

    public bool IsPlayerTurn(string connectionId, GameSession session)
    {
        var playerColor = session.GetPlayerColor(connectionId);
        return playerColor.HasValue &&
               session.Engine.CurrentPlayer?.Color == playerColor.Value;
    }

    public GameModeFeatures GetFeatures() => new()
    {
        AllowChat = true,
        AllowDouble = true,
        AllowImportExport = false,
        ShowAnalysisBadge = false
    };
}
