using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent periodically with current time state
/// </summary>
[TranspilationSource]
public class TimeUpdateDto
{
    public string GameId { get; set; } = string.Empty;

    public double WhiteReserveSeconds { get; set; }

    public double RedReserveSeconds { get; set; }

    public bool WhiteIsInDelay { get; set; }

    public bool RedIsInDelay { get; set; }

    public double WhiteDelayRemaining { get; set; }

    public double RedDelayRemaining { get; set; }
}
