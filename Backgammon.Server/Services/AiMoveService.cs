using Backgammon.AI;
using Backgammon.Core;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for handling AI opponent moves in backgammon games.
/// Supports both GreedyAI and RandomAI with realistic delays between actions.
/// </summary>
public class AiMoveService : IAiMoveService
{
    private const string GreedyAiPrefix = "ai_greedy_";
    private const string RandomAiPrefix = "ai_random_";
    private const int DelayBeforeRoll = 500;      // ms before rolling dice
    private const int DelayAfterRoll = 800;       // ms after rolling to show dice
    private const int DelayPerMove = 600;         // ms per move executed

    private readonly ILogger<AiMoveService> _logger;

    public AiMoveService(ILogger<AiMoveService> logger)
    {
        _logger = logger;
    }

    public bool IsAiPlayer(string? playerId)
    {
        if (playerId == null) return false;
        return playerId.StartsWith(GreedyAiPrefix) || playerId.StartsWith(RandomAiPrefix);
    }

    public string GenerateAiPlayerId(string aiType = "greedy")
    {
        var prefix = aiType.ToLowerInvariant() == "random" ? RandomAiPrefix : GreedyAiPrefix;
        return $"{prefix}{Guid.NewGuid()}";
    }

    /// <summary>
    /// Creates an AI instance based on the player ID prefix.
    /// </summary>
    private IBackgammonAI CreateAI(string playerId)
    {
        if (playerId.StartsWith(RandomAiPrefix))
        {
            return new RandomAI("RandomBot");
        }
        return new GreedyAI("GreedyBot");
    }

    public async Task ExecuteAiTurnAsync(GameSession session, string aiPlayerId, Func<Task> broadcastUpdate)
    {
        var engine = session.Engine;
        var ai = CreateAI(aiPlayerId);

        _logger.LogInformation("AI {AiPlayerId} starting turn in game {GameId}", aiPlayerId, session.Id);

        try
        {
            // Delay before rolling dice (AI "thinking")
            await Task.Delay(DelayBeforeRoll);

            // Roll dice
            engine.RollDice();
            _logger.LogInformation("AI rolled {Die1} and {Die2}", engine.Dice.Die1, engine.Dice.Die2);

            // Broadcast dice roll to clients
            await broadcastUpdate();

            // Delay after roll to show dice
            await Task.Delay(DelayAfterRoll);

            // Execute AI moves
            var validMoves = engine.GetValidMoves();
            int moveCount = 0;

            while (engine.RemainingMoves.Count > 0 && validMoves.Count > 0)
            {
                var move = SelectBestMove(validMoves, engine, ai);

                if (engine.ExecuteMove(move))
                {
                    moveCount++;
                    _logger.LogDebug("AI executed move: {From} -> {To}", move.From, move.To);

                    // Broadcast each move to clients
                    await broadcastUpdate();

                    // Delay between moves for visual effect
                    await Task.Delay(DelayPerMove);
                }
                else
                {
                    _logger.LogWarning("AI failed to execute move: {From} -> {To}", move.From, move.To);
                    break;
                }

                validMoves = engine.GetValidMoves();
            }

            _logger.LogInformation("AI completed {MoveCount} moves", moveCount);

            // End the AI's turn
            engine.EndTurn();
            await broadcastUpdate();

            _logger.LogInformation("AI turn completed in game {GameId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AI turn in game {GameId}", session.Id);
            throw;
        }
    }

    /// <summary>
    /// Selects the best move using the AI's strategy.
    /// For RandomAI: selects randomly.
    /// For GreedyAI: uses priority-based strategy (bear off > hit > advance).
    /// </summary>
    private Move SelectBestMove(List<Move> validMoves, GameEngine engine, IBackgammonAI ai)
    {
        // For RandomAI, select randomly
        if (ai is RandomAI)
        {
            var random = new Random();
            return validMoves[random.Next(validMoves.Count)];
        }

        // For GreedyAI (or any other AI), use greedy strategy
        // Priority 1: Bear off if possible
        var bearOffMoves = validMoves.Where(m => m.IsBearOff).ToList();
        if (bearOffMoves.Any())
        {
            // Prefer bearing off from the furthest point
            return bearOffMoves.OrderByDescending(m =>
                engine.CurrentPlayer.Color == CheckerColor.White ? m.From : 25 - m.From
            ).First();
        }

        // Priority 2: Hit opponent's blot if possible
        var hitMoves = new List<Move>();
        foreach (var move in validMoves)
        {
            if (move.From == 0) continue; // Skip bar entry for this check

            var toPoint = engine.Board.GetPoint(move.To);
            if (toPoint.IsBlot && toPoint.Color != engine.CurrentPlayer.Color)
            {
                hitMoves.Add(move);
            }
        }

        if (hitMoves.Any())
        {
            return hitMoves.First();
        }

        // Priority 3: Move checker that's furthest from home
        if (engine.CurrentPlayer.Color == CheckerColor.White)
        {
            return validMoves.OrderByDescending(m => m.From).First();
        }
        else
        {
            return validMoves.OrderBy(m => m.From).First();
        }
    }
}
