using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for GameHistory - tracking all turns in a game.
/// </summary>
public class GameHistoryTests
{
    [Fact]
    public void TurnCount_EmptyHistory_ReturnsZero()
    {
        // Arrange
        var history = new GameHistory();

        // Act & Assert
        Assert.Equal(0, history.TurnCount);
    }

    [Fact]
    public void TurnCount_WithTurns_ReturnsCorrectCount()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 2, Player = CheckerColor.Red });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 3, Player = CheckerColor.White });

        // Act & Assert
        Assert.Equal(3, history.TurnCount);
    }

    [Fact]
    public void GetTurn_ValidTurnNumber_ReturnsTurn()
    {
        // Arrange
        var history = new GameHistory();
        var turn1 = new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White };
        var turn2 = new TurnSnapshot { TurnNumber = 2, Player = CheckerColor.Red };
        history.Turns.Add(turn1);
        history.Turns.Add(turn2);

        // Act
        var result = history.GetTurn(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TurnNumber);
        Assert.Equal(CheckerColor.White, result.Player);
    }

    [Fact]
    public void GetTurn_InvalidTurnNumber_ReturnsNull()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });

        // Act
        var result = history.GetTurn(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTurn_EmptyHistory_ReturnsNull()
    {
        // Arrange
        var history = new GameHistory();

        // Act
        var result = history.GetTurn(1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPlayerTurns_WhitePlayer_ReturnsOnlyWhiteTurns()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 2, Player = CheckerColor.Red });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 3, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 4, Player = CheckerColor.Red });

        // Act
        var whiteTurns = history.GetPlayerTurns(CheckerColor.White);

        // Assert
        Assert.Equal(2, whiteTurns.Count);
        Assert.All(whiteTurns, t => Assert.Equal(CheckerColor.White, t.Player));
    }

    [Fact]
    public void GetPlayerTurns_RedPlayer_ReturnsOnlyRedTurns()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 2, Player = CheckerColor.Red });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 3, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 4, Player = CheckerColor.Red });

        // Act
        var redTurns = history.GetPlayerTurns(CheckerColor.Red);

        // Assert
        Assert.Equal(2, redTurns.Count);
        Assert.All(redTurns, t => Assert.Equal(CheckerColor.Red, t.Player));
    }

    [Fact]
    public void GetPlayerTurns_NoTurnsForPlayer_ReturnsEmptyList()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 3, Player = CheckerColor.White });

        // Act
        var redTurns = history.GetPlayerTurns(CheckerColor.Red);

        // Assert
        Assert.Empty(redTurns);
    }

    [Fact]
    public void Clear_RemovesAllTurns()
    {
        // Arrange
        var history = new GameHistory();
        history.Turns.Add(new TurnSnapshot { TurnNumber = 1, Player = CheckerColor.White });
        history.Turns.Add(new TurnSnapshot { TurnNumber = 2, Player = CheckerColor.Red });

        // Act
        history.Clear();

        // Assert
        Assert.Equal(0, history.TurnCount);
        Assert.Empty(history.Turns);
    }

    [Fact]
    public void Clear_EmptyHistory_DoesNotThrow()
    {
        // Arrange
        var history = new GameHistory();

        // Act & Assert (should not throw)
        history.Clear();

        Assert.Equal(0, history.TurnCount);
    }
}
