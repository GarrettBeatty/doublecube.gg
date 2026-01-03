using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Service for managing doubling cube operations
/// </summary>
public interface IDoubleOfferService
{
    /// <summary>
    /// Offer to double the stakes
    /// </summary>
    Task<(bool Success, int CurrentValue, int NewValue, string? Error)> OfferDoubleAsync(GameSession session, string connectionId);

    /// <summary>
    /// Accept a double offer
    /// </summary>
    Task<bool> AcceptDoubleAsync(GameSession session);

    /// <summary>
    /// Decline a double offer (opponent wins at current stakes)
    /// </summary>
    Task<(bool Success, Player? Winner, int Stakes, string? Error)> DeclineDoubleAsync(GameSession session, string connectionId);

    /// <summary>
    /// Handle AI response to a double offer
    /// </summary>
    Task<(bool Accepted, Player? Winner, int Stakes)> HandleAiDoubleResponseAsync(
        GameSession session,
        string opponentPlayerId,
        int currentValue,
        int newValue);
}
