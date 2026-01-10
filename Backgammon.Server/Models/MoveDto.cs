using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for a move
/// </summary>
[TranspilationSource]
public class MoveDto
{
    public int From { get; set; }

    public int To { get; set; }

    public int DieValue { get; set; }

    public bool IsHit { get; set; }
}
