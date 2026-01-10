using Tapper;

namespace Backgammon.Server.Models;

[TranspilationSource]
public enum GameStatus
{
    WaitingForPlayer,
    InProgress,
    Completed
}
