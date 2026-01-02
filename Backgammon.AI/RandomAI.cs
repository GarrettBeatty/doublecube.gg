using Backgammon.Core;

namespace Backgammon.AI;

/// <summary>
/// A simple AI that makes random valid moves
/// </summary>
public class RandomAI : IBackgammonAI
{
    private readonly Random _random;

    public RandomAI(string name = "RandomAI")
    {
        Name = name;
        _random = new Random();
    }

    public RandomAI(string name, int seed)
    {
        Name = name;
        _random = new Random(seed);
    }

    public string Name { get; }

    public List<Move> ChooseMoves(GameEngine engine)
    {
        var chosenMoves = new List<Move>();

        // Keep making moves until no valid moves remain
        while (engine.RemainingMoves.Count > 0)
        {
            var validMoves = engine.GetValidMoves();

            if (validMoves.Count == 0)
            {
                break;
            }

            // Choose a random valid move
            var move = validMoves[_random.Next(validMoves.Count)];

            // Execute it
            if (engine.ExecuteMove(move))
            {
                chosenMoves.Add(move);
            }
            else
            {
                // Should not happen if GetValidMoves is correct
                break;
            }
        }

        return chosenMoves;
    }

    public bool ShouldAcceptDouble(GameEngine engine)
    {
        // Accept 50% of the time randomly
        return _random.Next(2) == 0;
    }

    public bool ShouldOfferDouble(GameEngine engine)
    {
        // Don't offer doubles for simplicity
        return false;
    }
}
