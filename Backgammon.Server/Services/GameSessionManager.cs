using System.Linq;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

public interface IGameSessionManager
{
    /// <summary>
    /// Create a new game session with a specific ID
    /// </summary>
    GameSession CreateGame(string? gameId = null);
    
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
    /// </summary>
    GameSession JoinOrCreate(string playerId, string connectionId, string? gameId = null);
    
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

public class GameSessionManager : IGameSessionManager
{
    private readonly Dictionary<string, GameSession> _games = new();
    private readonly Dictionary<string, string> _playerToGame = new();
    private readonly object _lock = new object();
    
    public GameSession CreateGame(string? gameId = null)
    {
        lock (_lock)
        {
            gameId ??= Guid.NewGuid().ToString();
            
            if (_games.ContainsKey(gameId))
                throw new InvalidOperationException($"Game {gameId} already exists");
            
            var session = new GameSession(gameId);
            _games[gameId] = session;
            return session;
        }
    }
    
    public GameSession? GetGame(string gameId)
    {
        lock (_lock)
        {
            return _games.GetValueOrDefault(gameId);
        }
    }
    
    public GameSession? GetGameByPlayer(string connectionId)
    {
        lock (_lock)
        {
            if (_playerToGame.TryGetValue(connectionId, out var gameId))
            {
                return _games.GetValueOrDefault(gameId);
            }
            return null;
        }
    }
    
    public GameSession JoinOrCreate(string playerId, string connectionId, string? gameId = null)
    {
        lock (_lock)
        {
            // Check if player is already in a game (reconnection scenario)
            if (_playerToGame.TryGetValue(connectionId, out var existingGameId))
            {
                if (_games.TryGetValue(existingGameId, out var playerGame))
                {
                    playerGame.UpdatePlayerConnection(playerId, connectionId);
                    return playerGame; // Already in this game
                }
            }
            
            // If specific game ID provided, try to join that game
            if (!string.IsNullOrEmpty(gameId))
            {
                if (_games.TryGetValue(gameId, out var existingGame))
                {
                    // Try to join/reconnect to existing game
                    if (existingGame.AddPlayer(playerId, connectionId))
                    {
                        _playerToGame[connectionId] = gameId;
                        return existingGame;
                    }
                    throw new InvalidOperationException("Game is full");
                }
                
                // Create the game with the specified ID
                var newGame = CreateGame(gameId);
                newGame.AddPlayer(playerId, connectionId);
                _playerToGame[connectionId] = gameId;
                return newGame;
            }
            
            // When gameId is null, ALWAYS create a new game (no matchmaking)
            var game = CreateGame();
            game.AddPlayer(playerId, connectionId);
            _playerToGame[connectionId] = game.Id;
            return game;
        }
    }
    
    public void RemovePlayer(string connectionId)
    {
        lock (_lock)
        {
            if (_playerToGame.TryGetValue(connectionId, out var gameId))
            {
                if (_games.TryGetValue(gameId, out var game))
                {
                    game.RemovePlayer(connectionId);

                    // If game is now empty, remove it
                    if (game.WhitePlayerId == null && game.RedPlayerId == null)
                    {
                        _games.Remove(gameId);
                    }
                }

                _playerToGame.Remove(connectionId);
            }
        }
    }

    public void RemoveGame(string gameId)
    {
        lock (_lock)
        {
            if (_games.TryGetValue(gameId, out var game))
            {
                // Remove player-to-game mappings
                if (game.WhiteConnectionId != null)
                    _playerToGame.Remove(game.WhiteConnectionId);
                if (game.RedConnectionId != null)
                    _playerToGame.Remove(game.RedConnectionId);

                // Remove game from dictionary
                _games.Remove(gameId);
            }
        }
    }

    public IEnumerable<GameSession> GetAllGames()
    {
        lock (_lock)
        {
            // Exclude analysis games from lobby
            return _games.Values
                .Where(g => !g.IsAnalysisMode)
                .ToList();
        }
    }
    
    public IEnumerable<GameSession> GetPlayerGames(string playerId)
    {
        lock (_lock)
        {
            return _games.Values
                .Where(g => (g.WhitePlayerId == playerId || g.RedPlayerId == playerId) && g.Engine.Winner == null)
                .ToList();
        }
    }
    
    public void CleanupInactiveGames(TimeSpan maxInactivity)
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow - maxInactivity;
            var inactiveGames = _games.Where(kvp => kvp.Value.LastActivityAt < cutoff).ToList();

            foreach (var (gameId, game) in inactiveGames)
            {
                _games.Remove(gameId);

                if (game.WhitePlayerId != null)
                    _playerToGame.Remove(game.WhitePlayerId);
                if (game.RedPlayerId != null)
                    _playerToGame.Remove(game.RedPlayerId);
            }
        }
    }

    public bool IsPlayerOnline(string playerId)
    {
        lock (_lock)
        {
            // Check if player has an active game with a connection
            return _games.Values.Any(g =>
                (g.WhitePlayerId == playerId && g.WhiteConnectionId != null) ||
                (g.RedPlayerId == playerId && g.RedConnectionId != null));
        }
    }

    public GameSession? GetSession(string gameId)
    {
        return GetGame(gameId);
    }

    public async Task LoadActiveGamesAsync(IGameRepository repository)
    {
        lock (_lock)
        {
            var activeGames = repository.GetActiveGamesAsync().Result; // Sync wait inside lock

            Console.WriteLine($"[GameSessionManager] Loading {activeGames.Count} active games from database...");

            int successCount = 0;
            int failureCount = 0;

            foreach (var gameData in activeGames)
            {
                try
                {
                    var session = GameEngineMapper.FromGame(gameData);

                    // Add to in-memory storage
                    _games[session.Id] = session;

                    // Note: Connection IDs are empty - players must reconnect
                    // The _playerToGame mapping will be populated when players reconnect via JoinGame

                    successCount++;
                    Console.WriteLine($"  ✓ Loaded game {session.Id}: " +
                                    $"White={session.WhitePlayerName ?? "waiting"}, " +
                                    $"Red={session.RedPlayerName ?? "waiting"}, " +
                                    $"Current={session.Engine.CurrentPlayer?.Name ?? "unknown"}");
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"  ✗ Failed to load game {gameData.GameId}: {ex.Message}");
                }
            }

            Console.WriteLine($"[GameSessionManager] Successfully loaded {successCount} games ({failureCount} failures)");
        }
    }
}
