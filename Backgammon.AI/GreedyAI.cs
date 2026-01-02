using Backgammon.Core;

namespace Backgammon.AI;

/// <summary>
/// An AI that prioritizes bearing off and hitting opponent checkers
/// </summary>
public class GreedyAI : IBackgammonAI
{
    public GreedyAI(string name = "GreedyAI")
    {
        Name = name;
    }

    public string Name { get; }

    public List<Move> ChooseMoves(GameEngine engine)
    {
        var chosenMoves = new List<Move>();

        while (engine.RemainingMoves.Count > 0)
        {
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

        return chosenMoves;
    }

    public bool ShouldAcceptDouble(GameEngine engine)
    {
        // Simple heuristic: accept if we've borne off at least some checkers
        // or opponent hasn't borne off many
        var opponent = engine.GetOpponent();

        if (engine.CurrentPlayer.CheckersBornOff >= 5)
        {
            return true;
        }

        if (opponent.CheckersBornOff < 10)
        {
            return true;
        }

        return false;
    }

    public bool ShouldOfferDouble(GameEngine engine)
    {
        // Offer double if we're ahead in bearing off
        var opponent = engine.GetOpponent();
        return engine.CurrentPlayer.CheckersBornOff > opponent.CheckersBornOff + 3;
    }

    private Move SelectBestMove(List<Move> validMoves, GameEngine engine)
    {
        // Priority 1: Bear off if possible
        var bearOffMoves = validMoves.Where(m => m.IsBearOff).ToList();
        if (bearOffMoves.Any())
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

        if (hitMoves.Any())
        {
            // Return the first hitting move
            return hitMoves.First();
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
