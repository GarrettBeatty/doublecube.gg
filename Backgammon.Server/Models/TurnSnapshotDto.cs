using System.Text.Json.Serialization;
using Backgammon.Core;
using Tapper;

namespace Backgammon.Server.Models;

/// <summary>
/// Data transfer object for turn snapshots, used for database serialization
/// </summary>
[TranspilationSource]
public class TurnSnapshotDto
{
    [JsonPropertyName("turnNumber")]
    public int TurnNumber { get; set; }

    [JsonPropertyName("player")]
    public string Player { get; set; } = string.Empty;

    [JsonPropertyName("diceRolled")]
    public int[] DiceRolled { get; set; } = Array.Empty<int>();

    [JsonPropertyName("positionSgf")]
    public string PositionSgf { get; set; } = string.Empty;

    [JsonPropertyName("moves")]
    public List<string> Moves { get; set; } = new();

    [JsonPropertyName("doublingAction")]
    public string? DoublingAction { get; set; }

    [JsonPropertyName("cubeValue")]
    public int CubeValue { get; set; } = 1;

    [JsonPropertyName("cubeOwner")]
    public string? CubeOwner { get; set; }

    /// <summary>
    /// Convert from Core.GameTurn (parsed from SGF) to DTO
    /// </summary>
    public static TurnSnapshotDto FromGameTurn(GameTurn turn)
    {
        return new TurnSnapshotDto
        {
            TurnNumber = turn.TurnNumber,
            Player = turn.Player.ToString(),
            DiceRolled = turn.Die1 > 0 && turn.Die2 > 0
                ? (turn.Die1 == turn.Die2 ? new[] { turn.Die1, turn.Die1, turn.Die1, turn.Die1 } : new[] { turn.Die1, turn.Die2 })
                : Array.Empty<int>(),
            PositionSgf = turn.PositionSgf ?? string.Empty,
            Moves = turn.Moves.Select(m =>
            {
                if (m.IsBearOff)
                {
                    return $"{m.From}/off";
                }

                if (m.From == 0)
                {
                    return $"bar/{m.To}";
                }

                return $"{m.From}/{m.To}";
            }).ToList(),
            DoublingAction = turn.CubeAction?.ToString(),
            CubeValue = 1, // Cube tracking would need separate state
            CubeOwner = null
        };
    }

    /// <summary>
    /// Convert from Core.TurnSnapshot to DTO
    /// </summary>
    public static TurnSnapshotDto FromCore(TurnSnapshot turn)
    {
        return new TurnSnapshotDto
        {
            TurnNumber = turn.TurnNumber,
            Player = turn.Player.ToString(),
            DiceRolled = turn.DiceRolled,
            PositionSgf = turn.PositionSgf,
            Moves = turn.Moves.Select(m =>
            {
                if (m.IsBearOff)
                {
                    return $"{m.From}/off";
                }

                if (m.From == 0)
                {
                    return $"bar/{m.To}";
                }

                return $"{m.From}/{m.To}";
            }).ToList(),
            DoublingAction = turn.DoublingAction?.ToString(),
            CubeValue = turn.CubeValue,
            CubeOwner = turn.CubeOwner
        };
    }

    /// <summary>
    /// Convert from DTO to Core.TurnSnapshot
    /// </summary>
    public TurnSnapshot ToCore()
    {
        var turn = new TurnSnapshot
        {
            TurnNumber = TurnNumber,
            Player = Enum.Parse<CheckerColor>(Player),
            DiceRolled = DiceRolled,
            PositionSgf = PositionSgf,
            CubeValue = CubeValue,
            CubeOwner = CubeOwner
        };

        if (!string.IsNullOrEmpty(DoublingAction))
        {
            turn.DoublingAction = Enum.Parse<Core.DoublingAction>(DoublingAction);
        }

        // Parse move notation back to Move objects
        foreach (var moveStr in Moves)
        {
            var parts = moveStr.Split('/');
            if (parts.Length == 2)
            {
                int from = parts[0] == "bar" ? 0 : int.Parse(parts[0]);
                int to = parts[1] == "off" ? (turn.Player == CheckerColor.White ? 0 : 25) : int.Parse(parts[1]);

                // Calculate die value from move
                int dieValue;
                if (from == 0)
                {
                    // Entering from bar
                    dieValue = turn.Player == CheckerColor.White ? 25 - to : to;
                }
                else if (to == 0 || to == 25)
                {
                    // Bearing off
                    dieValue = turn.Player == CheckerColor.White ? from : 25 - from;
                }
                else
                {
                    // Normal move
                    dieValue = Math.Abs(to - from);
                }

                turn.Moves.Add(new Move(from, to, dieValue));
            }
        }

        return turn;
    }
}
