using Backgammon.Core;
using Backgammon.Server.Models;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services;

/// <summary>
/// Implementation of doubling cube operations
/// </summary>
public class DoubleOfferService : IDoubleOfferService
{
    private readonly ILogger<DoubleOfferService> _logger;

    public DoubleOfferService(ILogger<DoubleOfferService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Success, int CurrentValue, int NewValue, string? Error)> OfferDoubleAsync(
        GameSession session,
        string connectionId)
    {
        if (!session.IsPlayerTurn(connectionId))
        {
            return (false, 0, 0, "Not your turn");
        }

        // Can only double before rolling dice
        if (session.Engine.RemainingMoves.Count > 0 || session.Engine.Dice.Die1 != 0)
        {
            return (false, 0, 0, "Can only double before rolling dice");
        }

        // Get current cube value before doubling
        var currentValue = session.Engine.DoublingCube.Value;

        // Check if player can offer double
        if (!session.Engine.OfferDouble())
        {
            return (false, 0, 0, "Cannot offer double - opponent owns the cube");
        }

        var newValue = currentValue * 2;

        // Set pending double state - get player ID from connection
        var playerColor = session.GetPlayerColor(connectionId);
        var playerId = playerColor == CheckerColor.White ? session.WhitePlayerId : session.RedPlayerId;
        if (!string.IsNullOrEmpty(playerId))
        {
            session.SetPendingDoubleOffer(playerId);
        }

        _logger.LogInformation(
            "Player {ConnectionId} offered double in game {GameId}. Stakes: {Current}x → {New}x",
            connectionId,
            session.Id,
            currentValue,
            newValue);

        return (true, currentValue, newValue, null);
    }

    public async Task<bool> AcceptDoubleAsync(GameSession session)
    {
        session.Engine.AcceptDouble();
        session.ClearPendingDoubleOffer();
        session.UpdateActivity();

        _logger.LogInformation(
            "Double accepted in game {GameId}. New stakes: {Stakes}x",
            session.Id,
            session.Engine.DoublingCube.Value);

        return true;
    }

    public async Task<(bool Success, Player? Winner, int Stakes, string? Error)> DeclineDoubleAsync(
        GameSession session,
        string connectionId)
    {
        // Determine declining player and opponent
        var decliningColor = session.GetPlayerColor(connectionId);
        if (decliningColor == null)
        {
            return (false, null, 0, "You are not a player in this game");
        }

        var decliningPlayer = decliningColor == CheckerColor.White
            ? session.Engine.WhitePlayer
            : session.Engine.RedPlayer;
        var opponentPlayer = decliningColor == CheckerColor.White
            ? session.Engine.RedPlayer
            : session.Engine.WhitePlayer;

        // Check if game is already over or hasn't started
        if (session.Engine.GameOver)
        {
            _logger.LogWarning("Game {GameId} is already over, cannot decline double", session.Id);
            return (false, null, 0, "Game is already finished");
        }

        if (!session.Engine.GameStarted)
        {
            _logger.LogWarning("Game {GameId} hasn't started yet, cannot decline double", session.Id);
            return (false, null, 0, "Game hasn't started yet");
        }

        // Clear pending double state before ending game
        session.ClearPendingDoubleOffer();

        // Decline double - opponent wins at current stakes (no gammon/backgammon multiplier)
        session.Engine.DeclineDouble(opponentPlayer);

        // Get stakes from current cube value (just 1x multiplier for declined double)
        var stakes = session.Engine.GetGameResult();

        _logger.LogInformation(
            "Game {GameId} ended - player declined double. Winner: {Winner} (Stakes: {Stakes})",
            session.Id,
            opponentPlayer.Name,
            stakes);

        return (true, opponentPlayer, stakes, null);
    }

    public async Task<(bool Accepted, Player? Winner, int Stakes)> HandleAiDoubleResponseAsync(
        GameSession session,
        string opponentPlayerId,
        int currentValue,
        int newValue)
    {
        _logger.LogInformation(
            "AI opponent {AiPlayerId} evaluating double offer in game {GameId}. Stakes: {Current}x → {New}x",
            opponentPlayerId,
            session.Id,
            currentValue,
            newValue);

        // AI decision logic: Accept if new value <= 4, otherwise decline
        // This is a simple conservative strategy
        bool aiAccepts = newValue <= 4;

        // Small delay to make it feel more natural
        await Task.Delay(1000);

        if (aiAccepts)
        {
            _logger.LogInformation("AI {AiPlayerId} accepted the double", opponentPlayerId);
            session.Engine.AcceptDouble();
            session.ClearPendingDoubleOffer();
            session.UpdateActivity();

            _logger.LogInformation(
                "AI accepted double in game {GameId}. New stakes: {Stakes}x",
                session.Id,
                session.Engine.DoublingCube.Value);

            return (true, null, 0);
        }
        else
        {
            _logger.LogInformation("AI {AiPlayerId} declined the double", opponentPlayerId);

            // Clear pending double state before ending game
            session.ClearPendingDoubleOffer();

            // Determine human player (we need to find who offered the double)
            // The human player is the one who's NOT the AI
            var humanColor = session.WhitePlayerId == opponentPlayerId
                ? CheckerColor.Red
                : CheckerColor.White;

            var humanPlayer = humanColor == CheckerColor.White
                ? session.Engine.WhitePlayer
                : session.Engine.RedPlayer;

            // AI declines - human wins at current stakes (no gammon/backgammon multiplier)
            session.Engine.DeclineDouble(humanPlayer);
            var stakes = session.Engine.GetGameResult();

            _logger.LogInformation(
                "Game {GameId} ended. AI declined double. Winner: {Winner} (Stakes: {Stakes})",
                session.Id,
                humanPlayer.Name,
                stakes);

            return (false, humanPlayer, stakes);
        }
    }
}
