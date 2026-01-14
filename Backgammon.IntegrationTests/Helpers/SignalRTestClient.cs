using System.Collections.Concurrent;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Backgammon.IntegrationTests.Helpers;

/// <summary>
/// Test wrapper around SignalR HubConnection that captures events for assertions.
/// </summary>
public class SignalRTestClient : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ConcurrentBag<GameState> _gameUpdates = new();
    private readonly ConcurrentBag<GameState> _gameStarts = new();
    private readonly ConcurrentBag<GameState> _gameOvers = new();
    private readonly ConcurrentBag<string> _errors = new();
    private readonly ConcurrentBag<string> _infos = new();
    private readonly ConcurrentBag<string> _waitingForOpponentGameIds = new();
    private readonly ConcurrentBag<DoubleOfferDto> _doubleOffers = new();
    private readonly ConcurrentBag<MatchCreatedDto> _matchCreated = new();
    private readonly ConcurrentBag<MatchGameCompletedDto> _matchGameCompleted = new();
    private readonly ConcurrentBag<MatchCompletedDto> _matchCompleted = new();
    private readonly ConcurrentBag<MatchContinuedDto> _matchContinued = new();
    private TaskCompletionSource<GameState>? _gameStartTcs;
    private TaskCompletionSource<GameState>? _gameOverTcs;
    private TaskCompletionSource<GameState>? _gameUpdateTcs;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignalRTestClient"/> class.
    /// </summary>
    /// <param name="connection">The SignalR hub connection to wrap.</param>
    public SignalRTestClient(HubConnection connection)
    {
        _connection = connection;
        RegisterEventHandlers();
    }

    /// <summary>
    /// Gets all captured GameUpdate events.
    /// </summary>
    public IReadOnlyCollection<GameState> GameUpdates => _gameUpdates.ToArray();

    /// <summary>
    /// Gets all captured GameStart events.
    /// </summary>
    public IReadOnlyCollection<GameState> GameStarts => _gameStarts.ToArray();

    /// <summary>
    /// Gets all captured GameOver events.
    /// </summary>
    public IReadOnlyCollection<GameState> GameOvers => _gameOvers.ToArray();

    /// <summary>
    /// Gets all captured Error events.
    /// </summary>
    public IReadOnlyCollection<string> Errors => _errors.ToArray();

    /// <summary>
    /// Gets all captured Info events.
    /// </summary>
    public IReadOnlyCollection<string> Infos => _infos.ToArray();

    /// <summary>
    /// Gets the most recent GameState from any update.
    /// </summary>
    public GameState? LatestState => _gameUpdates.LastOrDefault() ?? _gameStarts.LastOrDefault();

    /// <summary>
    /// Gets the underlying HubConnection state.
    /// </summary>
    public HubConnectionState ConnectionState => _connection.State;

    /// <summary>
    /// Starts the SignalR connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ConnectAsync()
    {
        await _connection.StartAsync();
    }

    /// <summary>
    /// Stops the SignalR connection.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DisconnectAsync()
    {
        await _connection.StopAsync();
    }

    /// <summary>
    /// Creates a new game and waits for WaitingForOpponent event.
    /// </summary>
    /// <returns>The game ID of the created game.</returns>
    public async Task<string> CreateGameAsync()
    {
        var beforeCount = _waitingForOpponentGameIds.Count;
        await _connection.InvokeAsync("CreateGame");

        // Wait for WaitingForOpponent event
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (_waitingForOpponentGameIds.Count <= beforeCount && DateTime.UtcNow < timeout)
        {
            await Task.Delay(50);
        }

        return _waitingForOpponentGameIds.LastOrDefault() ?? throw new TimeoutException("Did not receive WaitingForOpponent event");
    }

    /// <summary>
    /// Joins an existing game.
    /// </summary>
    /// <param name="gameId">The game ID to join.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task JoinGameAsync(string gameId)
    {
        await _connection.InvokeAsync("JoinGame", gameId);
    }

    /// <summary>
    /// Rolls the dice.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task RollDiceAsync()
    {
        await _connection.InvokeAsync("RollDice");
    }

    /// <summary>
    /// Makes a move from one position to another.
    /// </summary>
    /// <param name="from">The source position.</param>
    /// <param name="to">The destination position.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task MakeMoveAsync(int from, int to)
    {
        await _connection.InvokeAsync("MakeMove", from, to);
    }

    /// <summary>
    /// Ends the current turn.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task EndTurnAsync()
    {
        await _connection.InvokeAsync("EndTurn");
    }

    /// <summary>
    /// Offers to double the stakes.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task OfferDoubleAsync()
    {
        await _connection.InvokeAsync("OfferDouble");
    }

    /// <summary>
    /// Accepts a double offer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AcceptDoubleAsync()
    {
        await _connection.InvokeAsync("AcceptDouble");
    }

    /// <summary>
    /// Declines a double offer.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task DeclineDoubleAsync()
    {
        await _connection.InvokeAsync("DeclineDouble");
    }

    /// <summary>
    /// Abandons the current game.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AbandonGameAsync()
    {
        await _connection.InvokeAsync("AbandonGame");
    }

    /// <summary>
    /// Creates a new match.
    /// </summary>
    /// <param name="friendId">Optional friend ID to invite.</param>
    /// <param name="targetScore">The target score for the match.</param>
    /// <param name="timeControlType">Optional time control type.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateMatchAsync(string? friendId, int targetScore, string? timeControlType = null)
    {
        await _connection.InvokeAsync("CreateMatchLobby", friendId, targetScore, timeControlType);
    }

    /// <summary>
    /// Joins an existing match.
    /// </summary>
    /// <param name="matchId">The match ID to join.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task JoinMatchAsync(string matchId)
    {
        await _connection.InvokeAsync("JoinMatch", matchId);
    }

    /// <summary>
    /// Continues to the next game in a match.
    /// </summary>
    /// <param name="matchId">The match ID to continue.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ContinueMatchAsync(string matchId)
    {
        await _connection.InvokeAsync("ContinueMatch", matchId);
    }

    /// <summary>
    /// Waits for a GameStart event.
    /// </summary>
    /// <param name="timeout">Optional timeout duration.</param>
    /// <returns>The game state from the GameStart event.</returns>
    public Task<GameState> WaitForGameStartAsync(TimeSpan? timeout = null)
    {
        _gameStartTcs = new TaskCompletionSource<GameState>();
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
        cts.Token.Register(() => _gameStartTcs.TrySetException(new TimeoutException("Timed out waiting for GameStart")));
        return _gameStartTcs.Task;
    }

    /// <summary>
    /// Waits for a GameOver event.
    /// </summary>
    /// <param name="timeout">Optional timeout duration.</param>
    /// <returns>The game state from the GameOver event.</returns>
    public Task<GameState> WaitForGameOverAsync(TimeSpan? timeout = null)
    {
        _gameOverTcs = new TaskCompletionSource<GameState>();
        var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        cts.Token.Register(() => _gameOverTcs.TrySetException(new TimeoutException("Timed out waiting for GameOver")));
        return _gameOverTcs.Task;
    }

    /// <summary>
    /// Waits for a GameUpdate event matching the specified predicate.
    /// </summary>
    /// <param name="predicate">Optional predicate to match.</param>
    /// <param name="timeout">Optional timeout duration.</param>
    /// <returns>The matching game state.</returns>
    public async Task<GameState> WaitForUpdateAsync(
        Func<GameState, bool>? predicate = null,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(5));

        while (DateTime.UtcNow < deadline)
        {
            var matching = _gameUpdates.LastOrDefault(predicate ?? (_ => true));
            if (matching != null)
            {
                return matching;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Timed out waiting for matching GameUpdate");
    }

    /// <summary>
    /// Waits for an Error event.
    /// </summary>
    /// <param name="timeout">Optional timeout duration.</param>
    /// <returns>The error message.</returns>
    public async Task<string> WaitForErrorAsync(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(5));
        var initialCount = _errors.Count;

        while (DateTime.UtcNow < deadline)
        {
            if (_errors.Count > initialCount)
            {
                return _errors.Last();
            }

            await Task.Delay(50);
        }

        throw new TimeoutException("Timed out waiting for Error event");
    }

    /// <summary>
    /// Waits a short time to check that no error occurs.
    /// </summary>
    /// <param name="waitTime">Optional wait time.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AssertNoErrorAsync(TimeSpan? waitTime = null)
    {
        var initialCount = _errors.Count;
        await Task.Delay(waitTime ?? TimeSpan.FromMilliseconds(200));

        if (_errors.Count > initialCount)
        {
            throw new Exception($"Unexpected error: {_errors.Last()}");
        }
    }

    /// <summary>
    /// Clears all captured events.
    /// </summary>
    public void ClearEvents()
    {
        while (_gameUpdates.TryTake(out _))
        {
        }

        while (_gameStarts.TryTake(out _))
        {
        }

        while (_gameOvers.TryTake(out _))
        {
        }

        while (_errors.TryTake(out _))
        {
        }

        while (_infos.TryTake(out _))
        {
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
        {
            await _connection.StopAsync();
        }

        await _connection.DisposeAsync();
    }

    private void RegisterEventHandlers()
    {
        _connection.On<GameState>("GameUpdate", state =>
        {
            _gameUpdates.Add(state);
            _gameUpdateTcs?.TrySetResult(state);
            _gameUpdateTcs = null;
        });

        _connection.On<GameState>("GameStart", state =>
        {
            _gameStarts.Add(state);
            _gameStartTcs?.TrySetResult(state);
            _gameStartTcs = null;
        });

        _connection.On<GameState>("GameOver", state =>
        {
            _gameOvers.Add(state);
            _gameOverTcs?.TrySetResult(state);
            _gameOverTcs = null;
        });

        _connection.On<string>("WaitingForOpponent", gameId =>
        {
            _waitingForOpponentGameIds.Add(gameId);
        });

        _connection.On<string>("Error", error =>
        {
            _errors.Add(error);
        });

        _connection.On<string>("Info", info =>
        {
            _infos.Add(info);
        });

        _connection.On<DoubleOfferDto>("DoubleOffered", offer =>
        {
            _doubleOffers.Add(offer);
        });

        _connection.On<MatchCreatedDto>("MatchCreated", data =>
        {
            _matchCreated.Add(data);
        });

        _connection.On<MatchGameCompletedDto>("MatchGameCompleted", data =>
        {
            _matchGameCompleted.Add(data);
        });

        _connection.On<MatchCompletedDto>("MatchCompleted", data =>
        {
            _matchCompleted.Add(data);
        });

        _connection.On<MatchContinuedDto>("MatchContinued", data =>
        {
            _matchContinued.Add(data);
        });
    }
}
