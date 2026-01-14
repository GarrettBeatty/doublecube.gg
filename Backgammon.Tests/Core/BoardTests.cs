using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for Board - the backgammon board with 24 points.
/// </summary>
public class BoardTests
{
    [Fact]
    public void Constructor_InitializesAllPoints()
    {
        // Arrange & Act
        var board = new Board();

        // Assert - all points 1-24 should be initialized
        for (int i = 1; i <= 24; i++)
        {
            var point = board.GetPoint(i);
            Assert.NotNull(point);
            Assert.Equal(i, point.Position);
            Assert.Empty(point.Checkers);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(25)]
    [InlineData(100)]
    public void GetPoint_InvalidPosition_ThrowsArgumentOutOfRangeException(int position)
    {
        // Arrange
        var board = new Board();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetPoint(position));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    [InlineData(24)]
    public void GetPoint_ValidPosition_ReturnsPoint(int position)
    {
        // Arrange
        var board = new Board();

        // Act
        var point = board.GetPoint(position);

        // Assert
        Assert.NotNull(point);
        Assert.Equal(position, point.Position);
    }

    [Fact]
    public void SetupInitialPosition_SetsCorrectWhiteCheckers()
    {
        // Arrange
        var board = new Board();

        // Act
        board.SetupInitialPosition();

        // Assert White positions
        Assert.Equal(CheckerColor.White, board.GetPoint(24).Color);
        Assert.Equal(2, board.GetPoint(24).Count);

        Assert.Equal(CheckerColor.White, board.GetPoint(13).Color);
        Assert.Equal(5, board.GetPoint(13).Count);

        Assert.Equal(CheckerColor.White, board.GetPoint(8).Color);
        Assert.Equal(3, board.GetPoint(8).Count);

        Assert.Equal(CheckerColor.White, board.GetPoint(6).Color);
        Assert.Equal(5, board.GetPoint(6).Count);
    }

    [Fact]
    public void SetupInitialPosition_SetsCorrectRedCheckers()
    {
        // Arrange
        var board = new Board();

        // Act
        board.SetupInitialPosition();

        // Assert Red positions
        Assert.Equal(CheckerColor.Red, board.GetPoint(1).Color);
        Assert.Equal(2, board.GetPoint(1).Count);

        Assert.Equal(CheckerColor.Red, board.GetPoint(12).Color);
        Assert.Equal(5, board.GetPoint(12).Count);

        Assert.Equal(CheckerColor.Red, board.GetPoint(17).Color);
        Assert.Equal(3, board.GetPoint(17).Count);

        Assert.Equal(CheckerColor.Red, board.GetPoint(19).Color);
        Assert.Equal(5, board.GetPoint(19).Count);
    }

    [Fact]
    public void SetupInitialPosition_ClearsExistingCheckers()
    {
        // Arrange
        var board = new Board();
        board.GetPoint(10).AddChecker(CheckerColor.White);
        board.GetPoint(15).AddChecker(CheckerColor.Red);

        // Act
        board.SetupInitialPosition();

        // Assert - points 10 and 15 should be empty
        Assert.Empty(board.GetPoint(10).Checkers);
        Assert.Empty(board.GetPoint(15).Checkers);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_WhiteWithCheckersOnBar_ReturnsFalse()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.White, "White");
        player.CheckersOnBar = 1;

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, player.CheckersOnBar);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_WhiteAllInHomeBoard_ReturnsTrue()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.White, "White");

        // Place all checkers in White's home board (points 1-6)
        for (int i = 0; i < 5; i++)
        {
            board.GetPoint(1).AddChecker(CheckerColor.White);
            board.GetPoint(2).AddChecker(CheckerColor.White);
            board.GetPoint(3).AddChecker(CheckerColor.White);
        }

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, 0);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_WhiteWithCheckerOutside_ReturnsFalse()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.White, "White");

        // Place one checker outside home board
        board.GetPoint(7).AddChecker(CheckerColor.White);

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, 0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_RedWithCheckersOnBar_ReturnsFalse()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.Red, "Red");
        player.CheckersOnBar = 1;

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, player.CheckersOnBar);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_RedAllInHomeBoard_ReturnsTrue()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.Red, "Red");

        // Place all checkers in Red's home board (points 19-24)
        for (int i = 0; i < 5; i++)
        {
            board.GetPoint(19).AddChecker(CheckerColor.Red);
            board.GetPoint(20).AddChecker(CheckerColor.Red);
            board.GetPoint(21).AddChecker(CheckerColor.Red);
        }

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, 0);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AreAllCheckersInHomeBoard_RedWithCheckerOutside_ReturnsFalse()
    {
        // Arrange
        var board = new Board();
        var player = new Player(CheckerColor.Red, "Red");

        // Place one checker outside home board
        board.GetPoint(18).AddChecker(CheckerColor.Red);

        // Act
        var result = board.AreAllCheckersInHomeBoard(player, 0);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetHighestPoint_WhiteWithCheckers_ReturnsHighest()
    {
        // Arrange
        var board = new Board();
        board.GetPoint(3).AddChecker(CheckerColor.White);
        board.GetPoint(5).AddChecker(CheckerColor.White);
        board.GetPoint(1).AddChecker(CheckerColor.White);

        // Act
        var highest = board.GetHighestPoint(CheckerColor.White);

        // Assert - point 5 is highest in White's home board
        Assert.Equal(5, highest);
    }

    [Fact]
    public void GetHighestPoint_WhiteNoCheckers_ReturnsZero()
    {
        // Arrange
        var board = new Board();

        // Act
        var highest = board.GetHighestPoint(CheckerColor.White);

        // Assert
        Assert.Equal(0, highest);
    }

    [Fact]
    public void GetHighestPoint_RedWithCheckers_ReturnsHighest()
    {
        // Arrange
        var board = new Board();
        board.GetPoint(19).AddChecker(CheckerColor.Red);
        board.GetPoint(22).AddChecker(CheckerColor.Red);
        board.GetPoint(24).AddChecker(CheckerColor.Red);

        // Act
        var highest = board.GetHighestPoint(CheckerColor.Red);

        // Assert - For Red, "highest" means furthest from bear off (point 19 is furthest from 24/25)
        // The scan goes from 19 to 24, returning first checker found
        Assert.Equal(19, highest);
    }

    [Fact]
    public void GetHighestPoint_RedNoCheckers_ReturnsZero()
    {
        // Arrange
        var board = new Board();

        // Act
        var highest = board.GetHighestPoint(CheckerColor.Red);

        // Assert
        Assert.Equal(0, highest);
    }

    [Fact]
    public void CountCheckers_EmptyBoard_ReturnsZero()
    {
        // Arrange
        var board = new Board();

        // Act
        var whiteCount = board.CountCheckers(CheckerColor.White);
        var redCount = board.CountCheckers(CheckerColor.Red);

        // Assert
        Assert.Equal(0, whiteCount);
        Assert.Equal(0, redCount);
    }

    [Fact]
    public void CountCheckers_WithCheckers_ReturnsCorrectCount()
    {
        // Arrange
        var board = new Board();
        board.GetPoint(1).AddChecker(CheckerColor.White);
        board.GetPoint(1).AddChecker(CheckerColor.White);
        board.GetPoint(6).AddChecker(CheckerColor.White);
        board.GetPoint(19).AddChecker(CheckerColor.Red);

        // Act
        var whiteCount = board.CountCheckers(CheckerColor.White);
        var redCount = board.CountCheckers(CheckerColor.Red);

        // Assert
        Assert.Equal(3, whiteCount);
        Assert.Equal(1, redCount);
    }

    [Fact]
    public void CountCheckers_InitialPosition_Returns15Each()
    {
        // Arrange
        var board = new Board();
        board.SetupInitialPosition();

        // Act
        var whiteCount = board.CountCheckers(CheckerColor.White);
        var redCount = board.CountCheckers(CheckerColor.Red);

        // Assert
        Assert.Equal(15, whiteCount);
        Assert.Equal(15, redCount);
    }
}
