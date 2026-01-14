using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for Dice - representing a dice roll in the game.
/// </summary>
public class DiceTests
{
    [Fact]
    public void Constructor_InitialValues_AreZero()
    {
        // Act
        var dice = new Dice();

        // Assert
        Assert.Equal(0, dice.Die1);
        Assert.Equal(0, dice.Die2);
    }

    [Fact]
    public void SetDice_SetsValues()
    {
        // Arrange
        var dice = new Dice();

        // Act
        dice.SetDice(3, 5);

        // Assert
        Assert.Equal(3, dice.Die1);
        Assert.Equal(5, dice.Die2);
    }

    [Fact]
    public void IsDoubles_SameValues_ReturnsTrue()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(4, 4);

        // Assert
        Assert.True(dice.IsDoubles);
    }

    [Fact]
    public void IsDoubles_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(3, 5);

        // Assert
        Assert.False(dice.IsDoubles);
    }

    [Fact]
    public void GetMoves_NormalRoll_ReturnsTwoMoves()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(3, 5);

        // Act
        var moves = dice.GetMoves();

        // Assert
        Assert.Equal(2, moves.Count);
        Assert.Contains(3, moves);
        Assert.Contains(5, moves);
    }

    [Fact]
    public void GetMoves_Doubles_ReturnsFourMoves()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(4, 4);

        // Act
        var moves = dice.GetMoves();

        // Assert
        Assert.Equal(4, moves.Count);
        Assert.All(moves, m => Assert.Equal(4, m));
    }

    [Fact]
    public void Roll_ProducesValidValues()
    {
        // Arrange
        var dice = new Dice();

        // Act - roll multiple times to test randomness
        for (int i = 0; i < 100; i++)
        {
            dice.Roll();

            // Assert - values should be 1-6
            Assert.InRange(dice.Die1, 1, 6);
            Assert.InRange(dice.Die2, 1, 6);
        }
    }

    [Fact]
    public void RollSingle_ProducesValidValue()
    {
        // Arrange
        var dice = new Dice();

        // Act - roll multiple times to test randomness
        for (int i = 0; i < 100; i++)
        {
            var value = dice.RollSingle();

            // Assert - value should be 1-6
            Assert.InRange(value, 1, 6);
        }
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(3, 5);

        // Act
        var result = dice.ToString();

        // Assert
        Assert.Equal("[3] [5]", result);
    }

    [Fact]
    public void ToString_Doubles_FormatsCorrectly()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(6, 6);

        // Act
        var result = dice.ToString();

        // Assert
        Assert.Equal("[6] [6]", result);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 6)]
    [InlineData(6, 1)]
    [InlineData(6, 6)]
    [InlineData(3, 4)]
    public void SetDice_AllValidCombinations(int die1, int die2)
    {
        // Arrange
        var dice = new Dice();

        // Act
        dice.SetDice(die1, die2);

        // Assert
        Assert.Equal(die1, dice.Die1);
        Assert.Equal(die2, dice.Die2);
    }

    [Fact]
    public void SetDice_ZeroValues_Allowed()
    {
        // Arrange - zero is used to clear dice
        var dice = new Dice();
        dice.SetDice(3, 5);

        // Act
        dice.SetDice(0, 0);

        // Assert
        Assert.Equal(0, dice.Die1);
        Assert.Equal(0, dice.Die2);
    }

    [Fact]
    public void GetMoves_ReturnsNewList()
    {
        // Arrange
        var dice = new Dice();
        dice.SetDice(3, 5);

        // Act
        var moves1 = dice.GetMoves();
        var moves2 = dice.GetMoves();

        // Assert - should be different list instances
        Assert.NotSame(moves1, moves2);
    }
}
