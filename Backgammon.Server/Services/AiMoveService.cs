using Backgammon.AI;
using Backgammon.Core;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for handling AI opponent moves in backgammon games.
/// Uses GreedyAI for move selection with realistic delays between actions.
/// </summary>
public class AiMoveService : IAiMoveService
{
    private const string AiPlayerIdPrefix = "ai_greedy_";
    private const int DelayBeforeRoll = 500;      // ms before rolling dice
    private const int DelayAfterRoll = 800;       // ms after rolling to show dice
    private const int DelayPerMove = 600;         // ms per move executed

    private readonly ILogger<AiMoveService> _logger;
    private readonly GreedyAI _ai;

    public AiMoveService(ILogger<AiMoveService> logger)
    {
        _logger = logger;
        _ai = new GreedyAI("Computer");
    }

    public bool IsAiPlayer(string? playerId)
    {
        return playerId?.StartsWith(AiPlayerIdPrefix) == true;
    }

    public string GenerateAiPlayerId()
    {
        return $"{AiPlayerIdPrefix}{Guid.NewGuid()}";
    }

    public async Task ExecuteAiTurnAsync(GameSession session, string aiPlayerId, Func<Task> broadcastUpdate)
    {
        var engine = session.Engine;

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
                var move = SelectBestMove(validMoves, engine);

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
    /// Selects the best move using GreedyAI's strategy.
    /// Extracted from GreedyAI to allow per-move broadcasting.
    /// </summary>
    private Move SelectBestMove(List<Move> validMoves, GameEngine engine)
    {
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
