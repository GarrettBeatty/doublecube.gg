using Backgammon.Server.Services.Results;

namespace Backgammon.Server.Hubs.Handlers;

/// <summary>
/// Handler for doubling cube operations.
/// </summary>
public interface IDoublingHandler
{
    /// <summary>
    /// Offer to double the stakes.
    /// </summary>
    Task<Result> OfferDoubleAsync(string connectionId);

    /// <summary>
    /// Accept a double offer.
    /// </summary>
    Task<Result> AcceptDoubleAsync(string connectionId);

    /// <summary>
    /// Decline a double offer (opponent wins at current stakes).
    /// </summary>
    Task<Result> DeclineDoubleAsync(string connectionId);
}
