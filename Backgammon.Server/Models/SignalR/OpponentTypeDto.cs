using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Match opponent type options
/// </summary>
[TranspilationSource]
public enum OpponentTypeDto
{
    Friend,
    AI,
    OpenLobby
}
