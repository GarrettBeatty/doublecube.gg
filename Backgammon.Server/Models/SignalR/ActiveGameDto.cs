using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data for an active game returned by GetActiveGames
/// </summary>
[TranspilationSource]
public class ActiveGameDto
{
    public string MatchId { get; set; } = string.Empty;

    public string? GameId { get; set; }

    public string Player1Name { get; set; } = string.Empty;

    public string Player2Name { get; set; } = string.Empty;

    public int Player1Rating { get; set; }

    public int Player2Rating { get; set; }

    public string CurrentPlayer { get; set; } = string.Empty;

    public string MyColor { get; set; } = string.Empty;

    public bool IsYourTurn { get; set; }

    public string MatchScore { get; set; } = string.Empty;

    public int MatchLength { get; set; }

    public string TimeControl { get; set; } = string.Empty;

    public int CubeValue { get; set; }

    public string CubeOwner { get; set; } = string.Empty;

    public bool IsCrawford { get; set; }

    public int Viewers { get; set; }

    public ActiveGameBoardPointDto[]? Board { get; set; }

    public int WhiteCheckersOnBar { get; set; }

    public int RedCheckersOnBar { get; set; }

    public int WhiteBornOff { get; set; }

    public int RedBornOff { get; set; }
}
