using Backgammon.Core;
using Backgammon.Plugins.Abstractions;

namespace Backgammon.AI.Bots;

/// <summary>
/// A simple bot that makes random valid moves.
/// Useful as a baseline for testing and as a beginner opponent.
/// </summary>
public class RandomBot : IGameBot
{
    private readonly Random _random;

    public RandomBot()
    {
        _random = new Random();
    }

    public RandomBot(int seed)
    {
        _random = new Random(seed);
    }

    public string BotId => "random";

    public string DisplayName => "Random Bot";

    public string Description => "Makes random valid moves. Good for beginners.";

    public int EstimatedElo => 800;

    public Task<List<Move>> ChooseMovesAsync(GameEngine engine, CancellationToken ct = default)
    {
        var chosenMoves = new List<Move>();

        // Keep making moves until no valid moves remain
        while (engine.RemainingMoves.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

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

        return Task.FromResult(chosenMoves);
    }

    public Task<bool> ShouldAcceptDoubleAsync(GameEngine engine, CancellationToken ct = default)
    {
        // Accept 50% of the time randomly
        return Task.FromResult(_random.Next(2) == 0);
    }

    public Task<bool> ShouldOfferDoubleAsync(GameEngine engine, CancellationToken ct = default)
    {
        // Don't offer doubles for simplicity
        return Task.FromResult(false);
    }
}
