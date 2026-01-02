namespace Backgammon.Server.Services;

public interface IGameSessionManager
{
    /// <summary>
    /// Create a new game session with a specific ID
    /// </summary>
    GameSession CreateGame(string? gameId = null);

    /// <summary>
    /// Register a player's connection to a game session (for manual game creation)
    /// </summary>
    void RegisterPlayerConnection(string connectionId, string gameId);

    /// <summary>
    /// Get a game session by ID
    /// </summary>
    GameSession? GetGame(string gameId);

    /// <summary>
    /// Get the game session a player is in
    /// </summary>
    GameSession? GetGameByPlayer(string connectionId);

    /// <summary>
    /// Join an existing game by ID, or create a new game (no matchmaking)
    /// Loads from database if game not in memory but exists as InProgress
    /// </summary>
    Task<GameSession> JoinOrCreateAsync(string playerId, string connectionId, string? gameId = null);

    /// <summary>
    /// Remove a player from their current game
    /// </summary>
    void RemovePlayer(string connectionId);

    /// <summary>
    /// Remove a game session completely
    /// </summary>
    void RemoveGame(string gameId);

    /// <summary>
    /// Get all active game sessions
    /// </summary>
    IEnumerable<GameSession> GetAllGames();

    /// <summary>
    /// Get all active games for a specific player
    /// </summary>
    IEnumerable<GameSession> GetPlayerGames(string playerId);

    /// <summary>
    /// Clean up old/abandoned games
    /// </summary>
    void CleanupInactiveGames(TimeSpan maxInactivity);

    /// <summary>
    /// Check if a player is currently online (has an active connection)
    /// </summary>
    bool IsPlayerOnline(string playerId);

    /// <summary>
    /// Get a game session by ID (alias for GetGame for clarity)
    /// </summary>
    GameSession? GetSession(string gameId);

    /// <summary>
    /// Load all active games from the database on server startup
    /// </summary>
    Task LoadActiveGamesAsync(IGameRepository repository);
}
