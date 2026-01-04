using System.Linq;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

public class GameSessionManager : IGameSessionManager
{
    private readonly Dictionary<string, GameSession> _games = new();
    private readonly Dictionary<string, string> _playerToGame = new();
    private readonly object _lock = new object();
    private readonly IGameRepository _gameRepository;

    public GameSessionManager(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public GameSession CreateGame(string? gameId = null)
    {
        lock (_lock)
        {
            gameId ??= Guid.NewGuid().ToString();

            if (_games.ContainsKey(gameId))
            {
                throw new InvalidOperationException($"Game {gameId} already exists");
            }

            var session = new GameSession(gameId);
            _games[gameId] = session;
            return session;
        }
    }

    /// <summary>
    /// Register a player's connection to a game session (for manual game creation)
    /// </summary>
    public void RegisterPlayerConnection(string connectionId, string gameId)
    {
        lock (_lock)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                _playerToGame[connectionId] = gameId;
            }
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

    public async Task<GameSession> JoinOrCreateAsync(string playerId, string connectionId, string? gameId = null)
    {
        // Check memory first (inside lock)
        lock (_lock)
        {
            // Check if player is already in a game
            if (_playerToGame.TryGetValue(connectionId, out var existingGameId))
            {
                // If trying to join the SAME game, this is a reconnection
                if (gameId == existingGameId && _games.TryGetValue(existingGameId, out var playerGame))
                {
                    playerGame.UpdatePlayerConnection(playerId, connectionId);
                    return playerGame; // Already in this game
                }

                // If trying to join a DIFFERENT game, remove from old game first
                if (!string.IsNullOrEmpty(gameId) && gameId != existingGameId)
                {
                    if (_games.TryGetValue(existingGameId, out var oldGame))
                    {
                        oldGame.RemovePlayer(connectionId);
                        _playerToGame.Remove(connectionId);
                    }
                }
            }

            // If specific game ID provided, try to join that game from memory
            if (!string.IsNullOrEmpty(gameId))
            {
                if (_games.TryGetValue(gameId, out var existingGame))
                {
                    // Try to join/reconnect to existing game
                    // If game is full, return it anyway - GameHub will add as spectator
                    if (existingGame.AddPlayer(playerId, connectionId))
                    {
                        _playerToGame[connectionId] = gameId;
                    }
                    else
                    {
                        // Game is full, but register connection for spectator tracking
                        _playerToGame[connectionId] = gameId;
                    }

                    return existingGame;
                }

                // Game not in memory - will check database outside lock
            }
        }

        // If specific gameId provided but not in memory, try database (outside lock to avoid blocking)
        if (!string.IsNullOrEmpty(gameId))
        {
            var game = await _gameRepository.GetGameByGameIdAsync(gameId);

            if (game != null && game.Status == "InProgress")
            {
                // Verify player is part of this game
                if (game.WhitePlayerId == playerId || game.RedPlayerId == playerId)
                {
                    lock (_lock)
                    {
                        // Double-check not loaded by another thread
                        if (!_games.ContainsKey(gameId))
                        {
                            var session = GameEngineMapper.FromGame(game);
                            _games[gameId] = session;
                        }

                        var loadedSession = _games[gameId];
                        loadedSession.AddPlayer(playerId, connectionId);
                        _playerToGame[connectionId] = gameId;
                        loadedSession.UpdateActivity();
                        return loadedSession;
                    }
                }

                throw new InvalidOperationException("You are not a player in this game");
            }

            // If game exists in DB but is not InProgress, don't allow joining
            if (game != null)
            {
                throw new InvalidOperationException($"This game has already ended (Status: {game.Status})");
            }

            // Game doesn't exist - security check to prevent arbitrary game ID creation via URL
            // Games can only be created via gameId=null (auto-generated ID) or explicit creation methods
            throw new InvalidOperationException($"Game {gameId} not found");
        }

        // When gameId is null, ALWAYS create a new game (no matchmaking)
        lock (_lock)
        {
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
                // Remove all player-to-game mappings
                foreach (var connectionId in game.WhiteConnections)
                {
                    _playerToGame.Remove(connectionId);
                }

                foreach (var connectionId in game.RedConnections)
                {
                    _playerToGame.Remove(connectionId);
                }

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
                {
                    _playerToGame.Remove(game.WhitePlayerId);
                }

                if (game.RedPlayerId != null)
                {
                    _playerToGame.Remove(game.RedPlayerId);
                }
            }
        }
    }

    public bool IsPlayerOnline(string playerId)
    {
        lock (_lock)
        {
            // Check if player has an active game with at least one connection
            return _games.Values.Any(g =>
                (g.WhitePlayerId == playerId && g.WhiteConnections.Count > 0) ||
                (g.RedPlayerId == playerId && g.RedConnections.Count > 0));
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
