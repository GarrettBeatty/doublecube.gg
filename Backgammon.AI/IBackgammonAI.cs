using Backgammon.Core;

namespace Backgammon.AI;

/// <summary>
/// Interface for Backgammon AI players
/// </summary>
public interface IBackgammonAI
{
    /// <summary>
    /// The name of the AI
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Choose moves for the current turn given the game state
    /// </summary>
    /// <param name="engine">The game engine with current state</param>
    /// <returns>List of moves to execute in order</returns>
    List<Move> ChooseMoves(GameEngine engine);

    /// <summary>
    /// Decide whether to accept a double or resign
    /// </summary>
    /// <param name="engine">The game engine with current state</param>
    /// <returns>True to accept the double, false to resign</returns>
    bool ShouldAcceptDouble(GameEngine engine);

    /// <summary>
    /// Decide whether to offer a double
    /// </summary>
    /// <param name="engine">The game engine with current state</param>
    /// <returns>True to offer a double</returns>
    bool ShouldOfferDouble(GameEngine engine);
}
