using Backgammon.Core;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;

namespace Backgammon.Server.Grains;

/// <summary>
/// Partial class for GameGrain: doubling cube actions.
/// </summary>
public partial class GameGrain
{
    /// <inheritdoc/>
    public async Task<ActionResult> OfferDoubleAsync(string connectionId)
    {
        if (_engine.Winner != null) return ActionResult.Error("Game is already completed");

        var playerColor = GetPlayerColorInternal(connectionId);
        if (playerColor == null) return ActionResult.Error("Not a player in this game");
        if (_isCrawfordGame ?? false) return ActionResult.Error("Cannot double during Crawford game");
        if (_hasPendingDoubleOffer) return ActionResult.Error("Double already pending");
        if (_engine.CurrentPlayer?.Color != playerColor) return ActionResult.Error("Not your turn");
        if (!_engine.DoublingCube.CanDouble(playerColor.Value)) return ActionResult.Error("Cannot double");

        var currentValue = _engine.DoublingCube.Value;
        var newValue = currentValue * 2;

        _hasPendingDoubleOffer = true;
        _pendingDoubleOfferedBy = GetPlayerIdForColor(playerColor);

        // Notify opponent
        var opponentConnections = playerColor == CheckerColor.White ? _redConnections : _whiteConnections;
        foreach (var opponentConnId in opponentConnections)
        {
            await _hubContext.Clients.Client(opponentConnId).DoubleOffered(new DoubleOfferDto
            {
                CurrentStakes = currentValue,
                NewStakes = newValue
            });
        }

        await BroadcastGameUpdateAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> AcceptDoubleAsync(string connectionId)
    {
        if (!_hasPendingDoubleOffer) return ActionResult.Error("No double offer pending");

        var playerColor = GetPlayerColorInternal(connectionId);
        if (playerColor == null) return ActionResult.Error("Not a player in this game");

        // The accepter is the one who was offered (not the offerer)
        if (GetPlayerIdForColor(playerColor) == _pendingDoubleOfferedBy)
            return ActionResult.Error("Cannot accept your own double offer");

        _engine.AcceptDouble();
        _hasPendingDoubleOffer = false;
        _pendingDoubleOfferedBy = null;

        _lastActivityAt = DateTime.UtcNow;

        await SaveGameAsync();
        await BroadcastDoubleAcceptedAsync();
        return ActionResult.Ok();
    }

    /// <inheritdoc/>
    public async Task<ActionResult> DeclineDoubleAsync(string connectionId)
    {
        if (!_hasPendingDoubleOffer) return ActionResult.Error("No double offer pending");

        var playerColor = GetPlayerColorInternal(connectionId);
        if (playerColor == null) return ActionResult.Error("Not a player in this game");

        if (GetPlayerIdForColor(playerColor) == _pendingDoubleOfferedBy)
            return ActionResult.Error("Cannot decline your own double offer");

        // Decliner loses — offerer wins
        var winnerColor = playerColor == CheckerColor.White ? CheckerColor.Red : CheckerColor.White;
        var winner = winnerColor == CheckerColor.White ? _engine.WhitePlayer : _engine.RedPlayer;
        _engine.ForfeitGame(winner);

        _hasPendingDoubleOffer = false;
        _pendingDoubleOfferedBy = null;

        await HandleGameCompletionAsync();
        return ActionResult.Ok();
    }
}
