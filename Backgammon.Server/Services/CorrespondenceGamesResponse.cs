namespace Backgammon.Server.Services;

/// <summary>
/// Response containing both "your turn" and "waiting" games
/// </summary>
public class CorrespondenceGamesResponse
{
    public List<CorrespondenceGameDto> YourTurnGames { get; set; } = new();

    public List<CorrespondenceGameDto> WaitingGames { get; set; } = new();

    public int TotalYourTurn { get; set; }

    public int TotalWaiting { get; set; }
}
