using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Backgammon.Server.Services.DynamoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ServerGame = Backgammon.Server.Models.Game;

namespace Backgammon.IntegrationTests.Database;

/// <summary>
/// Integration tests for GameRepository with real DynamoDB Local.
/// Tests game persistence, serialization, and query operations.
/// </summary>
[Collection("DynamoDB")]
[Trait("Category", "Integration")]
[Trait("Component", "DynamoDB")]
public class GameRepositoryTests : IAsyncLifetime
{
    private readonly DynamoDbFixture _fixture;
    private DynamoDbGameRepository _repository = null!;

    public GameRepositoryTests(DynamoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DynamoDb:TableName"] = _fixture.TableName
            })
            .Build();

        _repository = new DynamoDbGameRepository(
            _fixture.Client,
            config,
            NullLogger<DynamoDbGameRepository>.Instance);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ==================== Save and Retrieve Tests ====================

    [Fact]
    public async Task SaveGameAsync_NewGame_CanBeRetrieved()
    {
        // Arrange
        var game = CreateTestGame();

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.GameId.Should().Be(game.GameId);
        retrieved.WhitePlayerId.Should().Be(game.WhitePlayerId);
        retrieved.RedPlayerId.Should().Be(game.RedPlayerId);
        retrieved.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task SaveGameAsync_WithBoardState_PreservesAllPoints()
    {
        // Arrange
        var game = CreateTestGame();
        game.BoardState = CreateCustomBoardState();

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.BoardState.Should().HaveCount(24);

        // Verify specific positions
        var point6 = retrieved.BoardState.First(p => p.Position == 6);
        point6.Color.Should().Be("White");
        point6.Count.Should().Be(5);

        var point13 = retrieved.BoardState.First(p => p.Position == 13);
        point13.Color.Should().Be("White");
        point13.Count.Should().Be(5);
    }

    [Fact]
    public async Task SaveGameAsync_WithDoublingCube_PreservesValueAndOwner()
    {
        // Arrange
        var game = CreateTestGame();
        game.DoublingCubeValue = 4;
        game.DoublingCubeOwner = "White";

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.DoublingCubeValue.Should().Be(4);
        retrieved.DoublingCubeOwner.Should().Be("White");
    }

    [Fact]
    public async Task SaveGameAsync_WithRemainingMoves_PreservesMoves()
    {
        // Arrange
        var game = CreateTestGame();
        game.RemainingMoves = new List<int> { 3, 5 };
        game.Die1 = 3;
        game.Die2 = 5;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.RemainingMoves.Should().BeEquivalentTo(new[] { 3, 5 });
        retrieved.Die1.Should().Be(3);
        retrieved.Die2.Should().Be(5);
    }

    [Fact]
    public async Task SaveGameAsync_WithDoubles_PreservesFourMoves()
    {
        // Arrange
        var game = CreateTestGame();
        game.RemainingMoves = new List<int> { 4, 4, 4, 4 };
        game.Die1 = 4;
        game.Die2 = 4;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.RemainingMoves.Should().BeEquivalentTo(new[] { 4, 4, 4, 4 });
    }

    [Fact]
    public async Task SaveGameAsync_CompletedGame_PreservesWinnerAndStakes()
    {
        // Arrange
        var game = CreateTestGame();
        game.Status = "Completed";
        game.Winner = "White";
        game.WinType = "Gammon";
        game.Stakes = 4; // Gammon (2) x Cube (2)
        game.DoublingCubeValue = 2;
        game.CompletedAt = DateTime.UtcNow;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
        retrieved.Winner.Should().Be("White");
        retrieved.WinType.Should().Be("Gammon");
        retrieved.Stakes.Should().Be(4);
    }

    [Fact]
    public async Task SaveGameAsync_WithCheckersOnBar_PreservesBarCounts()
    {
        // Arrange
        var game = CreateTestGame();
        game.WhiteCheckersOnBar = 2;
        game.RedCheckersOnBar = 1;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WhiteCheckersOnBar.Should().Be(2);
        retrieved.RedCheckersOnBar.Should().Be(1);
    }

    [Fact]
    public async Task SaveGameAsync_WithBornOff_PreservesBornOffCounts()
    {
        // Arrange
        var game = CreateTestGame();
        game.WhiteBornOff = 10;
        game.RedBornOff = 8;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.WhiteBornOff.Should().Be(10);
        retrieved.RedBornOff.Should().Be(8);
    }

    [Fact]
    public async Task GetGameByGameIdAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetGameByGameIdAsync("non-existent-game-id");

        // Assert
        result.Should().BeNull();
    }

    // ==================== Player Game Index Tests ====================

    [Fact]
    public async Task SaveGameAsync_CreatesPlayerGameIndex_ForBothPlayers()
    {
        // Arrange
        var whitePlayerId = Guid.NewGuid().ToString();
        var redPlayerId = Guid.NewGuid().ToString();
        var game = CreateTestGame(whitePlayerId, redPlayerId);

        // Act
        await _repository.SaveGameAsync(game);

        // Both players should be able to find the game
        var whiteGames = await _repository.GetPlayerGamesAsync(whitePlayerId);
        var redGames = await _repository.GetPlayerGamesAsync(redPlayerId);

        // Assert
        whiteGames.Should().ContainSingle(g => g.GameId == game.GameId);
        redGames.Should().ContainSingle(g => g.GameId == game.GameId);
    }

    [Fact]
    public async Task GetPlayerGamesAsync_ReturnsAllPlayerGames()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();

        var game1 = CreateTestGame(playerId);
        var game2 = CreateTestGame(playerId);
        var game3 = CreateTestGame(playerId);

        await _repository.SaveGameAsync(game1);
        await _repository.SaveGameAsync(game2);
        await _repository.SaveGameAsync(game3);

        // Act
        var games = await _repository.GetPlayerGamesAsync(playerId);

        // Assert
        games.Should().HaveCount(3);
        games.Select(g => g.GameId).Should().Contain(game1.GameId);
        games.Select(g => g.GameId).Should().Contain(game2.GameId);
        games.Select(g => g.GameId).Should().Contain(game3.GameId);
    }

    [Fact]
    public async Task GetPlayerGamesAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();

        var inProgressGame = CreateTestGame(playerId);
        inProgressGame.Status = "InProgress";

        var completedGame = CreateTestGame(playerId);
        completedGame.Status = "Completed";
        completedGame.Winner = "White";

        await _repository.SaveGameAsync(inProgressGame);
        await _repository.SaveGameAsync(completedGame);

        // Act
        var inProgressGames = await _repository.GetPlayerGamesAsync(playerId, "InProgress");
        var completedGames = await _repository.GetPlayerGamesAsync(playerId, "Completed");

        // Assert
        inProgressGames.Should().ContainSingle(g => g.GameId == inProgressGame.GameId);
        completedGames.Should().ContainSingle(g => g.GameId == completedGame.GameId);
    }

    // ==================== Status Update Tests ====================

    [Fact]
    public async Task UpdateGameStatusAsync_ChangesStatus()
    {
        // Arrange
        var game = CreateTestGame();
        await _repository.SaveGameAsync(game);

        // Act
        await _repository.UpdateGameStatusAsync(game.GameId, "Completed");
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task UpdateGameStatusAsync_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var game = CreateTestGame();
        await _repository.SaveGameAsync(game);

        // Act
        await _repository.UpdateGameStatusAsync(game.GameId, "Completed");
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CompletedAt.Should().NotBeNull();
        retrieved.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ==================== Active Games Query Tests ====================

    [Fact]
    public async Task GetActiveGamesAsync_ReturnsOnlyInProgressGames()
    {
        // Arrange
        var inProgressGame = CreateTestGame();
        inProgressGame.Status = "InProgress";

        var completedGame = CreateTestGame();
        completedGame.Status = "Completed";
        completedGame.Winner = "White";

        await _repository.SaveGameAsync(inProgressGame);
        await _repository.SaveGameAsync(completedGame);

        // Act
        var activeGames = await _repository.GetActiveGamesAsync();

        // Assert
        activeGames.Should().Contain(g => g.GameId == inProgressGame.GameId);
        activeGames.Should().NotContain(g => g.GameId == completedGame.GameId);
    }

    // ==================== Delete Tests ====================

    [Fact]
    public async Task DeleteGameAsync_RemovesGame()
    {
        // Arrange
        var game = CreateTestGame();
        await _repository.SaveGameAsync(game);

        // Act
        await _repository.DeleteGameAsync(game.GameId);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGameAsync_RemovesPlayerGameIndexItems()
    {
        // Arrange
        var whitePlayerId = Guid.NewGuid().ToString();
        var redPlayerId = Guid.NewGuid().ToString();
        var game = CreateTestGame(whitePlayerId, redPlayerId);
        await _repository.SaveGameAsync(game);

        // Act
        await _repository.DeleteGameAsync(game.GameId);

        // Assert
        var whiteGames = await _repository.GetPlayerGamesAsync(whitePlayerId);
        var redGames = await _repository.GetPlayerGamesAsync(redPlayerId);

        whiteGames.Should().NotContain(g => g.GameId == game.GameId);
        redGames.Should().NotContain(g => g.GameId == game.GameId);
    }

    // ==================== Count Tests ====================

    [Fact]
    public async Task GetTotalGameCountAsync_WithStatus_ReturnsCorrectCount()
    {
        // Arrange - create games with unique IDs
        var prefix = Guid.NewGuid().ToString()[..8];

        for (int i = 0; i < 3; i++)
        {
            var game = CreateTestGame();
            game.GameId = $"{prefix}-inprogress-{i}";
            game.Status = "InProgress";
            await _repository.SaveGameAsync(game);
        }

        for (int i = 0; i < 2; i++)
        {
            var game = CreateTestGame();
            game.GameId = $"{prefix}-completed-{i}";
            game.Status = "Completed";
            game.Winner = "White";
            await _repository.SaveGameAsync(game);
        }

        // Act
        var inProgressCount = await _repository.GetTotalGameCountAsync("InProgress");
        var completedCount = await _repository.GetTotalGameCountAsync("Completed");

        // Assert
        inProgressCount.Should().BeGreaterThanOrEqualTo(3);
        completedCount.Should().BeGreaterThanOrEqualTo(2);
    }

    // ==================== Match Game Tests ====================

    [Fact]
    public async Task SaveGameAsync_MatchGame_PreservesMatchIdAndCrawfordFlag()
    {
        // Arrange
        var game = CreateTestGame();
        game.MatchId = Guid.NewGuid().ToString();
        game.IsCrawfordGame = true;

        // Act
        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.MatchId.Should().Be(game.MatchId);
        retrieved.IsCrawfordGame.Should().BeTrue();
    }

    // ==================== GameEngineMapper Round-Trip Tests ====================

    [Fact]
    public async Task GameEngineMapper_RoundTrip_PreservesCompleteGameState()
    {
        // Arrange - create a game session with specific state
        var session = new GameSession(Guid.NewGuid().ToString());
        session.AddPlayer(Guid.NewGuid().ToString(), "conn1");
        session.WhitePlayerName = "Alice";
        session.AddPlayer(Guid.NewGuid().ToString(), "conn2");
        session.RedPlayerName = "Bob";

        var engine = session.Engine;
        engine.StartNewGame();
        engine.Dice.SetDice(3, 5);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(new[] { 3, 5 });

        // Execute a move to change board state
        var moves = engine.GetValidMoves();
        if (moves.Count > 0)
        {
            engine.ExecuteMove(moves[0]);
        }

        // Act - convert to Game model, save, retrieve, convert back
        var game = GameEngineMapper.ToGame(session);
        await _repository.SaveGameAsync(game);

        var retrievedGame = await _repository.GetGameByGameIdAsync(game.GameId);
        retrievedGame.Should().NotBeNull();

        var restoredSession = GameEngineMapper.FromGame(retrievedGame!);

        // Assert
        var restoredEngine = restoredSession.Engine;

        // Verify board state
        for (int i = 1; i <= 24; i++)
        {
            var originalPoint = engine.Board.GetPoint(i);
            var restoredPoint = restoredEngine.Board.GetPoint(i);

            restoredPoint.Color.Should().Be(originalPoint.Color, $"Point {i} color mismatch");
            restoredPoint.Count.Should().Be(originalPoint.Count, $"Point {i} count mismatch");
        }

        // Verify player state
        restoredEngine.WhitePlayer.CheckersOnBar.Should().Be(engine.WhitePlayer.CheckersOnBar);
        restoredEngine.RedPlayer.CheckersOnBar.Should().Be(engine.RedPlayer.CheckersOnBar);
        restoredEngine.WhitePlayer.CheckersBornOff.Should().Be(engine.WhitePlayer.CheckersBornOff);
        restoredEngine.RedPlayer.CheckersBornOff.Should().Be(engine.RedPlayer.CheckersBornOff);

        // Verify dice and moves
        restoredEngine.Dice.Die1.Should().Be(engine.Dice.Die1);
        restoredEngine.Dice.Die2.Should().Be(engine.Dice.Die2);
        restoredEngine.RemainingMoves.Should().BeEquivalentTo(engine.RemainingMoves);

        // Verify current player
        restoredEngine.CurrentPlayer?.Color.Should().Be(engine.CurrentPlayer?.Color);
    }

    [Fact]
    public async Task GameEngineMapper_RoundTrip_ValidatesCheckerCount()
    {
        // This test verifies that GameEngineMapper.FromGame validates the checker count
        // A corrupted game should throw InvalidOperationException

        var session = new GameSession(Guid.NewGuid().ToString());
        session.AddPlayer(Guid.NewGuid().ToString(), "conn1");
        session.AddPlayer(Guid.NewGuid().ToString(), "conn2");
        session.Engine.StartNewGame();

        var game = GameEngineMapper.ToGame(session);

        // Corrupt the board state (remove checkers)
        game.BoardState[0].Count = 0; // This will make total < 15

        await _repository.SaveGameAsync(game);
        var retrieved = await _repository.GetGameByGameIdAsync(game.GameId);

        // Act & Assert
        var act = () => GameEngineMapper.FromGame(retrieved!);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*15*"); // Should mention expected 15 checkers
    }

    // ==================== Helper Methods ====================

    private static ServerGame CreateTestGame(string? whitePlayerId = null, string? redPlayerId = null)
    {
        return new ServerGame
        {
            GameId = Guid.NewGuid().ToString(),
            WhitePlayerId = whitePlayerId ?? Guid.NewGuid().ToString(),
            RedPlayerId = redPlayerId ?? Guid.NewGuid().ToString(),
            WhitePlayerName = "TestWhite",
            RedPlayerName = "TestRed",
            Status = "InProgress",
            GameStarted = true,
            CurrentPlayer = "White",
            BoardState = CreateInitialBoardState(),
            DoublingCubeValue = 1,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    private static List<PointStateDto> CreateInitialBoardState()
    {
        // Standard backgammon starting position
        var states = new List<PointStateDto>();

        for (int i = 1; i <= 24; i++)
        {
            states.Add(new PointStateDto { Position = i, Color = null, Count = 0 });
        }

        // White starting positions
        states[5] = new PointStateDto { Position = 6, Color = "White", Count = 5 };
        states[7] = new PointStateDto { Position = 8, Color = "White", Count = 3 };
        states[12] = new PointStateDto { Position = 13, Color = "White", Count = 5 };
        states[23] = new PointStateDto { Position = 24, Color = "White", Count = 2 };

        // Red starting positions
        states[0] = new PointStateDto { Position = 1, Color = "Red", Count = 2 };
        states[11] = new PointStateDto { Position = 12, Color = "Red", Count = 5 };
        states[16] = new PointStateDto { Position = 17, Color = "Red", Count = 3 };
        states[18] = new PointStateDto { Position = 19, Color = "Red", Count = 5 };

        return states;
    }

    private static List<PointStateDto> CreateCustomBoardState()
    {
        // Same as initial but can be modified for specific tests
        return CreateInitialBoardState();
    }
}
