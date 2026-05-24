using Backgammon.Core;
using Backgammon.Server.Services;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: analysis/study mode actions.
/// </summary>
public partial class GameGrain
{
    /// <inheritdoc/>
    public async Task<ActionResult> SetDiceAsync(string connectionId, int die1, int die2)
    {
        if (die1 < 1 || die1 > 6 || die2 < 1 || die2 > 6)
            return ActionResult.Error("Dice values must be between 1 and 6");

        var initialDiceCount = _engine.Dice.GetMoves().Count;
        var currentRemainingCount = _engine.RemainingMoves.Count;
        var noMovesLeft = currentRemainingCount == 0;
        var noMovesMadeYet = currentRemainingCount == initialDiceCount;

        if (!noMovesLeft && !noMovesMadeYet)
            return ActionResult.Error("End your turn or undo moves before setting new dice");

        _engine.StartTurnWithDice(die1, die2);
        _lastActivityAt = DateTime.UtcNow;
        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> MoveCheckerDirectlyAsync(string connectionId, int from, int to)
    {
        if (from < 0 || from > 25 || to < 0 || to > 25)
            return ActionResult.Error("Invalid point number");
        if (from == to)
            return ActionResult.Error("Source and destination must be different");

        // Determine which checker color to move based on the source
        CheckerColor? checkerColor = null;
        if (from == 0)
        {
            // Moving from bar
            if (_engine.WhitePlayer.CheckersOnBar > 0) checkerColor = CheckerColor.White;
            else if (_engine.RedPlayer.CheckersOnBar > 0) checkerColor = CheckerColor.Red;
        }
        else if (from == 25)
        {
            // Moving from bear-off
            if (_engine.WhitePlayer.CheckersBornOff > 0) checkerColor = CheckerColor.White;
            else if (_engine.RedPlayer.CheckersBornOff > 0) checkerColor = CheckerColor.Red;
        }
        else
        {
            var point = _engine.Board.GetPoint(from);
            if (point.Color.HasValue && point.Count > 0) checkerColor = point.Color;
        }

        if (checkerColor == null) return ActionResult.Error("No checker at source point");

        // Remove from source
        if (from == 0)
        {
            if (checkerColor == CheckerColor.White) _engine.WhitePlayer.CheckersOnBar--;
            else _engine.RedPlayer.CheckersOnBar--;
        }
        else if (from == 25)
        {
            if (checkerColor == CheckerColor.White) _engine.WhitePlayer.CheckersBornOff--;
            else _engine.RedPlayer.CheckersBornOff--;
        }
        else
        {
            var point = _engine.Board.GetPoint(from);
            point.Checkers.RemoveAt(point.Checkers.Count - 1);
        }

        // Add to destination
        if (to == 0)
        {
            if (checkerColor == CheckerColor.White) _engine.WhitePlayer.CheckersOnBar++;
            else _engine.RedPlayer.CheckersOnBar++;
        }
        else if (to == 25)
        {
            if (checkerColor == CheckerColor.White) _engine.WhitePlayer.CheckersBornOff++;
            else _engine.RedPlayer.CheckersBornOff++;
        }
        else
        {
            var point = _engine.Board.GetPoint(to);
            point.AddChecker(checkerColor.Value);
        }

        _lastActivityAt = DateTime.UtcNow;
        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> SetCurrentPlayerAsync(string connectionId, string color)
    {
        if (!Enum.TryParse<CheckerColor>(color, true, out var checkerColor))
            return ActionResult.Error("Invalid color");

        var player = checkerColor == CheckerColor.White ? _engine.WhitePlayer : _engine.RedPlayer;
        SetPrivateField(_engine, "<CurrentPlayer>k__BackingField", player);
        _engine.RemainingMoves.Clear();
        _lastActivityAt = DateTime.UtcNow;
        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public Task<string?> ExportPositionAsync()
    {
        try
        {
            var sgf = SgfSerializer.ExportPosition(_engine);
            return Task.FromResult<string?>(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf)));
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc/>
    public Task<string?> ExportGameSgfAsync()
    {
        try
        {
            var sgf = _engine.GameSgf;
            return Task.FromResult<string?>(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(sgf)));
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }

    /// <inheritdoc/>
    public async Task<ActionResult> ImportPositionAsync(string sgf)
    {
        try
        {
            string decoded;
            try
            {
                var bytes = Convert.FromBase64String(sgf);
                decoded = System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                decoded = sgf; // Treat as raw SGF if not base64
            }

            SgfSerializer.ImportPosition(_engine, decoded);
            _lastActivityAt = DateTime.UtcNow;
            await BroadcastGameUpdateAsync();
            return ActionResult.Ok();
        }
        catch (Exception ex)
        {
            return ActionResult.Error($"Failed to import position: {ex.Message}");
        }
    }
}
