namespace Backgammon.Server.Services;

/// <summary>
/// Result of a game action
/// </summary>
public class ActionResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public bool GameEnded { get; set; }

    public static ActionResult Ok() => new() { Success = true };

    public static ActionResult Error(string message) => new() { Success = false, ErrorMessage = message };

    public static ActionResult GameOver() => new() { Success = true, GameEnded = true };
}
