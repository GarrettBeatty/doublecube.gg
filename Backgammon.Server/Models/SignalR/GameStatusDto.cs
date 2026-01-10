using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Mirror enum for GameStatus
/// </summary>
[TranspilationSource]
public enum GameStatusDto
{
    WaitingForPlayer = 0,
    InProgress = 1,
    Completed = 2
}
