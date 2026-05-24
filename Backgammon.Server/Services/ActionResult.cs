using Orleans;

namespace Backgammon.Server.Services;

/// <summary>
/// Result of a game action
/// </summary>
[GenerateSerializer]
public class ActionResult
{
    [Id(0)]
    public bool Success { get; set; }

    [Id(1)]
    public string? ErrorMessage { get; set; }

    [Id(2)]
    public bool GameEnded { get; set; }

    public static ActionResult Ok() => new() { Success = true };

    public static ActionResult Error(string message) => new() { Success = false, ErrorMessage = message };

    public static ActionResult GameOver() => new() { Success = true, GameEnded = true };
}
