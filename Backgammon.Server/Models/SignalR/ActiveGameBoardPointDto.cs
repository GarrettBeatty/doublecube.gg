using Tapper;

namespace Backgammon.Server.Models.SignalR;

/// <summary>
/// Board point data for active game preview
/// </summary>
[TranspilationSource]
public class ActiveGameBoardPointDto
{
    public int Position { get; set; }

    public string? Color { get; set; }

    public int Count { get; set; }
}
