using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for TurnSnapshot - capturing turn state.
/// </summary>
public class TurnSnapshotTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var snapshot = new TurnSnapshot();

        // Assert
        Assert.Equal(0, snapshot.TurnNumber);
        Assert.Empty(snapshot.DiceRolled);
        Assert.Equal(string.Empty, snapshot.PositionSgf);
        Assert.Empty(snapshot.Moves);
        Assert.Null(snapshot.DoublingAction);
        Assert.Equal(1, snapshot.CubeValue);
        Assert.Null(snapshot.CubeOwner);
    }

    [Fact]
    public void DiceRolled_RegularRoll_StoredCorrectly()
    {
        // Arrange & Act
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 3, 5 }
        };

        // Assert
        Assert.Equal(2, snapshot.DiceRolled.Length);
        Assert.Equal(3, snapshot.DiceRolled[0]);
        Assert.Equal(5, snapshot.DiceRolled[1]);
    }

    [Fact]
    public void DiceRolled_Doubles_StoredCorrectly()
    {
        // Arrange & Act
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 4, 4, 4, 4 }
        };

        // Assert
        Assert.Equal(4, snapshot.DiceRolled.Length);
        Assert.All(snapshot.DiceRolled, d => Assert.Equal(4, d));
    }

    [Fact]
    public void Moves_CanStoreMultiple()
    {
        // Arrange & Act
        var snapshot = new TurnSnapshot
        {
            Moves = new List<Move>
            {
                new Move(24, 20, 4),
                new Move(13, 9, 4)
            }
        };

        // Assert
        Assert.Equal(2, snapshot.Moves.Count);
    }

    [Fact]
    public void PositionSgf_CanStoreFullPosition()
    {
        // Arrange
        var sgf = "(;GM[6]AW[a][b][c]AB[x][w][v]PL[W])";

        // Act
        var snapshot = new TurnSnapshot
        {
            PositionSgf = sgf
        };

        // Assert
        Assert.Equal(sgf, snapshot.PositionSgf);
    }

    // ==================== Die1/Die2 Computed Properties Tests ====================

    [Fact]
    public void Die1_RegularRoll_ReturnsFirstDie()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 3, 5 }
        };

        // Act & Assert
        Assert.Equal(3, snapshot.Die1);
    }

    [Fact]
    public void Die2_RegularRoll_ReturnsSecondDie()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 3, 5 }
        };

        // Act & Assert
        Assert.Equal(5, snapshot.Die2);
    }

    [Fact]
    public void Die1_Doubles_ReturnsFirstValue()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 4, 4, 4, 4 }
        };

        // Act & Assert
        Assert.Equal(4, snapshot.Die1);
    }

    [Fact]
    public void Die2_Doubles_ReturnsSecondValue()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 4, 4, 4, 4 }
        };

        // Act & Assert
        Assert.Equal(4, snapshot.Die2);
    }

    [Fact]
    public void Die1_EmptyDice_ReturnsZero()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = Array.Empty<int>()
        };

        // Act & Assert
        Assert.Equal(0, snapshot.Die1);
    }

    [Fact]
    public void Die2_EmptyDice_ReturnsZero()
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = Array.Empty<int>()
        };

        // Act & Assert
        Assert.Equal(0, snapshot.Die2);
    }

    [Fact]
    public void Die2_SingleDieOnly_ReturnsZero()
    {
        // Arrange - Edge case with only one die value
        var snapshot = new TurnSnapshot
        {
            DiceRolled = new[] { 6 }
        };

        // Act & Assert
        Assert.Equal(6, snapshot.Die1);
        Assert.Equal(0, snapshot.Die2);
    }

    [Theory]
    [InlineData(new[] { 1, 6 }, 1, 6)]
    [InlineData(new[] { 6, 1 }, 6, 1)]
    [InlineData(new[] { 2, 3 }, 2, 3)]
    [InlineData(new[] { 5, 5, 5, 5 }, 5, 5)]
    public void Die1Die2_VariousRolls_ReturnsCorrectValues(int[] diceRolled, int expectedDie1, int expectedDie2)
    {
        // Arrange
        var snapshot = new TurnSnapshot
        {
            DiceRolled = diceRolled
        };

        // Act & Assert
        Assert.Equal(expectedDie1, snapshot.Die1);
        Assert.Equal(expectedDie2, snapshot.Die2);
    }
}
