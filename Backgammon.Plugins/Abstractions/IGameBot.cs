using Backgammon.Core;

namespace Backgammon.Plugins.Abstractions;

/// <summary>
/// Unified interface for backgammon bots.
/// Bots make game decisions: move selection, cube decisions.
/// All methods are async to support both fast local bots and slower external evaluators.
/// </summary>
public interface IGameBot
{
    /// <summary>
    /// Unique identifier for this bot type (e.g., "random", "greedy", "gnubg-bot")
    /// </summary>
    string BotId { get; }

    /// <summary>
    /// Human-friendly display name
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Description of bot's play style
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Estimated ELO rating for matchmaking hints
    /// </summary>
    int EstimatedElo { get; }

    /// <summary>
    /// Choose moves for the current turn.
    /// The bot should execute moves on the engine until RemainingMoves is empty.
    /// </summary>
    /// <param name="engine">The game engine with current state and dice rolled</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of moves executed in order</returns>
    Task<List<Move>> ChooseMovesAsync(GameEngine engine, CancellationToken ct = default);

    /// <summary>
    /// Decide whether to accept a double or resign
    /// </summary>
    /// <param name="engine">The game engine with current state</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True to accept the double, false to resign</returns>
    Task<bool> ShouldAcceptDoubleAsync(GameEngine engine, CancellationToken ct = default);

    /// <summary>
    /// Decide whether to offer a double
    /// </summary>
    /// <param name="engine">The game engine with current state</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True to offer a double</returns>
    Task<bool> ShouldOfferDoubleAsync(GameEngine engine, CancellationToken ct = default);
}
