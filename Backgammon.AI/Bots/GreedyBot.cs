using Backgammon.Core;
using Backgammon.Plugins.Abstractions;

namespace Backgammon.AI.Bots;

/// <summary>
/// A bot that prioritizes bearing off and hitting opponent checkers.
/// Uses a simple greedy heuristic for move selection.
/// </summary>
public class GreedyBot : IGameBot
{
    public string BotId => "greedy";

    public string DisplayName => "Greedy Bot";

    public string Description => "Prioritizes bearing off, hitting blots, and advancing. Fast and reliable.";

    public int EstimatedElo => 1200;

    public Task<List<Move>> ChooseMovesAsync(GameEngine engine, CancellationToken ct = default)
    {
        var chosenMoves = new List<Move>();

        while (engine.RemainingMoves.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            var validMoves = engine.GetValidMoves();

            if (validMoves.Count == 0)
            {
                break;
            }

            // Choose the best move based on priorities
            var move = SelectBestMove(validMoves, engine);

            if (engine.ExecuteMove(move))
            {
                chosenMoves.Add(move);
            }
            else
            {
                break;
            }
        }

        return Task.FromResult(chosenMoves);
    }

    public Task<bool> ShouldAcceptDoubleAsync(GameEngine engine, CancellationToken ct = default)
    {
        // Simple heuristic: accept if we've borne off at least some checkers
        // or opponent hasn't borne off many
        var opponent = engine.GetOpponent();

        if (engine.CurrentPlayer.CheckersBornOff >= 5)
        {
            return Task.FromResult(true);
        }

        if (opponent.CheckersBornOff < 10)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> ShouldOfferDoubleAsync(GameEngine engine, CancellationToken ct = default)
    {
        // Offer double if we're ahead in bearing off
        var opponent = engine.GetOpponent();
        return Task.FromResult(engine.CurrentPlayer.CheckersBornOff > opponent.CheckersBornOff + 3);
    }

    private static Move SelectBestMove(List<Move> validMoves, GameEngine engine)
    {
        // Priority 1: Bear off if possible
        var bearOffMoves = validMoves.Where(m => m.IsBearOff).ToList();
        if (bearOffMoves.Count > 0)
        {
            // Prefer bearing off from the furthest point
            return bearOffMoves.OrderByDescending(m =>
                engine.CurrentPlayer.Color == CheckerColor.White ? m.From : 25 - m.From).First();
        }

        // Priority 2: Hit opponent's blot if possible
        var hitMoves = new List<Move>();
        foreach (var move in validMoves)
        {
            if (move.From == 0)
            {
                continue; // Skip bar entry for this check
            }

            var toPoint = engine.Board.GetPoint(move.To);
            if (toPoint.IsBlot && toPoint.Color != engine.CurrentPlayer.Color)
            {
                hitMoves.Add(move);
            }
        }

        if (hitMoves.Count > 0)
        {
            // Return the first hitting move
            return hitMoves[0];
        }

        // Priority 3: Move checker that's furthest from home
        if (engine.CurrentPlayer.Color == CheckerColor.White)
        {
            // White moves from high to low
            return validMoves.OrderByDescending(m => m.From).First();
        }
        else
        {
            // Red moves from low to high
            return validMoves.OrderBy(m => m.From).First();
        }
    }
}
