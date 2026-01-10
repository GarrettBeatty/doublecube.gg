namespace Backgammon.Server.Services;

/// <summary>
/// Response containing both "your turn" and "waiting" correspondence games
/// </summary>
public class CorrespondenceGamesResponse
{
    /// <summary>
    /// List of correspondence games where it's the player's turn to move
    /// </summary>
    public List<CorrespondenceGameDto> YourTurnGames { get; set; } = new();

    /// <summary>
    /// List of correspondence games where the player is waiting for opponent
    /// </summary>
    public List<CorrespondenceGameDto> WaitingGames { get; set; } = new();

    /// <summary>
    /// List of lobbies created by the player that are waiting for opponents to join
    /// </summary>
    public List<CorrespondenceGameDto> MyLobbies { get; set; } = new();

    /// <summary>
    /// Total count of games where it's the player's turn
    /// </summary>
    public int TotalYourTurn { get; set; }

    /// <summary>
    /// Total count of games where the player is waiting
    /// </summary>
    public int TotalWaiting { get; set; }

    /// <summary>
    /// Total count of lobbies created by the player waiting for opponents
    /// </summary>
    public int TotalMyLobbies { get; set; }
}
