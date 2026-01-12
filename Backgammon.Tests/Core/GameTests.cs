using Backgammon.Core;

namespace Backgammon.Tests.Core;

public class GameTests
{
    [Fact]
    public void Constructor_Default_SetsInProgressStatus()
    {
        // Act
        var game = new Game();

        // Assert
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Empty(game.GameId);
        Assert.Null(game.Winner);
        Assert.Equal(0, game.Stakes);
        Assert.Equal(WinType.Normal, game.WinType);
        Assert.Null(game.MatchId);
        Assert.False(game.IsCrawfordGame);
        Assert.Empty(game.MoveHistory);
    }

    [Fact]
    public void Constructor_WithGameId_SetsGameId()
    {
        // Arrange
        var gameId = "test-game-123";

        // Act
        var game = new Game(gameId);

        // Assert
        Assert.Equal(gameId, game.GameId);
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void GameId_CanBeSet()
    {
        // Arrange
        var game = new Game();
        var gameId = "new-game-id";

        // Act
        game.GameId = gameId;

        // Assert
        Assert.Equal(gameId, game.GameId);
    }

    [Fact]
    public void Winner_CanBeSet_White()
    {
        // Arrange
        var game = new Game();

        // Act
        game.Winner = CheckerColor.White;

        // Assert
        Assert.Equal(CheckerColor.White, game.Winner);
    }

    [Fact]
    public void Winner_CanBeSet_Red()
    {
        // Arrange
        var game = new Game();

        // Act
        game.Winner = CheckerColor.Red;

        // Assert
        Assert.Equal(CheckerColor.Red, game.Winner);
    }

    [Fact]
    public void Winner_CanBeNull()
    {
        // Arrange
        var game = new Game { Winner = CheckerColor.White };

        // Act
        game.Winner = null;

        // Assert
        Assert.Null(game.Winner);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Stakes_CanBeSet(int stakes)
    {
        // Arrange
        var game = new Game();

        // Act
        game.Stakes = stakes;

        // Assert
        Assert.Equal(stakes, game.Stakes);
    }

    [Theory]
    [InlineData(WinType.Normal)]
    [InlineData(WinType.Gammon)]
    [InlineData(WinType.Backgammon)]
    public void WinType_CanBeSet(WinType winType)
    {
        // Arrange
        var game = new Game();

        // Act
        game.WinType = winType;

        // Assert
        Assert.Equal(winType, game.WinType);
    }

    [Fact]
    public void MatchId_CanBeSet()
    {
        // Arrange
        var game = new Game();
        var matchId = "match-123";

        // Act
        game.MatchId = matchId;

        // Assert
        Assert.Equal(matchId, game.MatchId);
    }

    [Fact]
    public void MatchId_CanBeNull()
    {
        // Arrange
        var game = new Game { MatchId = "match-123" };

        // Act
        game.MatchId = null;

        // Assert
        Assert.Null(game.MatchId);
    }

    [Fact]
    public void IsCrawfordGame_CanBeSet_True()
    {
        // Arrange
        var game = new Game();

        // Act
        game.IsCrawfordGame = true;

        // Assert
        Assert.True(game.IsCrawfordGame);
    }

    [Fact]
    public void IsCrawfordGame_CanBeSet_False()
    {
        // Arrange
        var game = new Game { IsCrawfordGame = true };

        // Act
        game.IsCrawfordGame = false;

        // Assert
        Assert.False(game.IsCrawfordGame);
    }

    [Fact]
    public void MoveHistory_CanAddMoves()
    {
        // Arrange
        var game = new Game();
        var move1 = new Move(24, 20, 4);
        var move2 = new Move(13, 9, 4);

        // Act
        game.MoveHistory.Add(move1);
        game.MoveHistory.Add(move2);

        // Assert
        Assert.Equal(2, game.MoveHistory.Count);
        Assert.Contains(move1, game.MoveHistory);
        Assert.Contains(move2, game.MoveHistory);
    }

    [Theory]
    [InlineData(GameStatus.InProgress)]
    [InlineData(GameStatus.Completed)]
    [InlineData(GameStatus.Abandoned)]
    public void Status_CanBeSet(GameStatus status)
    {
        // Arrange
        var game = new Game();

        // Act
        game.Status = status;

        // Assert
        Assert.Equal(status, game.Status);
    }

    [Fact]
    public void CompletedGame_AllPropertiesSet()
    {
        // Arrange
        var gameId = "completed-game";
        var matchId = "match-456";
        var move = new Move(6, 0, 6);

        // Act
        var game = new Game(gameId)
        {
            Winner = CheckerColor.White,
            Stakes = 2,
            WinType = WinType.Gammon,
            MatchId = matchId,
            IsCrawfordGame = false,
            Status = GameStatus.Completed
        };
        game.MoveHistory.Add(move);

        // Assert
        Assert.Equal(gameId, game.GameId);
        Assert.Equal(CheckerColor.White, game.Winner);
        Assert.Equal(2, game.Stakes);
        Assert.Equal(WinType.Gammon, game.WinType);
        Assert.Equal(matchId, game.MatchId);
        Assert.NotNull(game.MatchId);
        Assert.False(game.IsCrawfordGame);
        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.Single(game.MoveHistory);
        Assert.Contains(move, game.MoveHistory);
    }

    [Fact]
    public void CrawfordMatchGame_PropertiesSet()
    {
        // Arrange & Act
        var game = new Game("crawford-game")
        {
            MatchId = "match-789",
            IsCrawfordGame = true
        };

        // Assert
        Assert.Equal("match-789", game.MatchId);
        Assert.NotNull(game.MatchId);
        Assert.True(game.IsCrawfordGame);
    }
}
