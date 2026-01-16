using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for handling AI opponent moves in backgammon games.
/// Delegates to IGameBot implementations via IBotResolver.
/// </summary>
public class AiMoveService : IAiMoveService
{
    private const int DelayBeforeRoll = 500;      // ms before rolling dice
    private const int DelayAfterRoll = 800;       // ms after rolling to show dice
    private const int DelayPerMove = 600;         // ms per move executed

    private readonly IBotResolver _botResolver;
    private readonly ILogger<AiMoveService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AiMoveService"/> class.
    /// </summary>
    /// <param name="botResolver">The bot resolver for looking up bots by player ID.</param>
    /// <param name="logger">The logger.</param>
    public AiMoveService(IBotResolver botResolver, ILogger<AiMoveService> logger)
    {
        _botResolver = botResolver;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsAiPlayer(string? playerId) => _botResolver.IsBot(playerId);

    /// <inheritdoc/>
    public string GenerateAiPlayerId(string aiType = "greedy")
    {
        var prefix = aiType.ToLowerInvariant() switch
        {
            "random" => "ai_random_",
            "gnubg" => "ai_gnubg_",
            _ => "ai_greedy_"
        };
        return $"{prefix}{Guid.NewGuid()}";
    }

    /// <inheritdoc/>
    public async Task ExecuteAiTurnAsync(GameSession session, string aiPlayerId, Func<Task> broadcastUpdate)
    {
        var bot = _botResolver.GetBot(aiPlayerId)
            ?? throw new InvalidOperationException($"No bot found for player ID: {aiPlayerId}");

        var engine = session.Engine;

        // Guard: Don't execute full AI turn during opening roll phase
        if (engine.IsOpeningRoll)
        {
            _logger.LogWarning(
                "ExecuteAiTurnAsync called during opening roll phase for game {GameId}. This should not happen - opening roll should use RollDiceAsync instead.",
                session.Id);
            return;
        }

        _logger.LogInformation("Bot {BotId} starting turn in game {GameId}", bot.BotId, session.Id);

        try
        {
            // Check if dice are already set (from opening roll)
            bool diceAlreadySet = engine.RemainingMoves.Count > 0;

            if (!diceAlreadySet)
            {
                // Delay before rolling dice (bot "thinking")
                await Task.Delay(DelayBeforeRoll);

                // Roll dice
                engine.RollDice();
                _logger.LogInformation("Bot rolled {Die1} and {Die2}", engine.Dice.Die1, engine.Dice.Die2);

                // Broadcast dice roll to clients
                await broadcastUpdate();

                // Delay after roll to show dice
                await Task.Delay(DelayAfterRoll);
            }
            else
            {
                _logger.LogInformation(
                    "Bot using opening roll dice: {Die1} and {Die2}",
                    engine.Dice.Die1,
                    engine.Dice.Die2);

                // Broadcast initial state with opening dice
                await broadcastUpdate();

                // Small delay before making moves
                await Task.Delay(DelayAfterRoll);
            }

            // Capture expected moves before bot executes
            var expectedMoveCount = engine.RemainingMoves.Count;
            var originalDice = new List<int>(engine.RemainingMoves);

            // Execute bot moves - single unified async path
            var moves = await bot.ChooseMovesAsync(engine);
            _logger.LogInformation("Bot chose {MoveCount} moves", moves.Count);

            // Detect if bot failed to use all dice (potential bug)
            if (engine.RemainingMoves.Count > 0)
            {
                var validMoves = engine.GetValidMoves();
                if (validMoves.Count > 0)
                {
                    // There are still valid moves - this is a bug!
                    _logger.LogError(
                        "Bot {BotId} failed to use all dice! Original dice: [{OriginalDice}], " +
                        "Remaining: [{Remaining}], Valid moves still available: [{ValidMoves}]",
                        bot.BotId,
                        string.Join(", ", originalDice),
                        string.Join(", ", engine.RemainingMoves),
                        string.Join(", ", validMoves.Select(m => $"{m.From}->{m.To}(die:{m.DieValue})")));
                    throw new InvalidOperationException(
                        $"Bot {bot.BotId} failed to execute all available moves. " +
                        $"This indicates a bug in move parsing or execution.");
                }
                else
                {
                    // No valid moves remain - player is blocked (legitimate)
                    _logger.LogInformation(
                        "Bot {BotId} could not use all dice - no valid moves available. " +
                        "Original dice: [{OriginalDice}], Remaining unused: [{Remaining}]",
                        bot.BotId,
                        string.Join(", ", originalDice),
                        string.Join(", ", engine.RemainingMoves));
                }
            }

            // Broadcast each move with delay for animation
            foreach (var move in moves)
            {
                _logger.LogDebug("Bot executed move: {From} -> {To}", move.From, move.To);
                await broadcastUpdate();
                await Task.Delay(DelayPerMove);
            }

            // End the bot's turn (with timer management)
            engine.EndTurnTimer();   // End bot's timer, consume reserve if needed
            engine.EndTurn();        // Switch to next player
            engine.StartTurnTimer(); // Start next player's timer
            await broadcastUpdate();

            _logger.LogInformation("Bot turn completed in game {GameId}", session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bot turn in game {GameId}", session.Id);
            throw;
        }
    }
}
