using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Mirror enum for CheckerColor (keeps Backgammon.Core pure without SignalR dependencies)
/// </summary>
[TranspilationSource]
public enum CheckerColorDto
{
    White = 0,
    Red = 1
}
