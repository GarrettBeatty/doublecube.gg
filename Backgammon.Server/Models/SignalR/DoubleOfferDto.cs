using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Data sent when a double offer is made
/// </summary>
[TranspilationSource]
public class DoubleOfferDto
{
    public int CurrentStakes { get; set; }

    public int NewStakes { get; set; }
}
