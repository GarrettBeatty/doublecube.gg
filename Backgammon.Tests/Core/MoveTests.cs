using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for Move - representing a checker move.
/// </summary>
public class MoveTests
{
    [Fact]
    public void Constructor_SingleDie_SetsProperties()
    {
        // Act
        var move = new Move(from: 13, to: 7, dieValue: 6, isHit: true);

        // Assert
        Assert.Equal(13, move.From);
        Assert.Equal(7, move.To);
        Assert.Equal(6, move.DieValue);
        Assert.True(move.IsHit);
        Assert.Null(move.DiceUsed);
        Assert.Null(move.IntermediatePoints);
    }

    [Fact]
    public void Constructor_SingleDie_DefaultIsHitFalse()
    {
        // Act
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Assert
        Assert.False(move.IsHit);
    }

    [Fact]
    public void Constructor_Combined_SetsProperties()
    {
        // Arrange
        var diceUsed = new[] { 6, 1 };
        var intermediatePoints = new[] { 18 };

        // Act
        var move = new Move(from: 24, to: 17, diceUsed: diceUsed, intermediatePoints: intermediatePoints, isHit: true);

        // Assert
        Assert.Equal(24, move.From);
        Assert.Equal(17, move.To);
        Assert.Equal(7, move.DieValue); // Sum of 6 + 1
        Assert.True(move.IsHit);
        Assert.Equal(diceUsed, move.DiceUsed);
        Assert.Equal(intermediatePoints, move.IntermediatePoints);
    }

    [Fact]
    public void Constructor_Combined_NullIntermediatePoints()
    {
        // Act
        var move = new Move(from: 24, to: 17, diceUsed: new[] { 6, 1 }, intermediatePoints: null);

        // Assert
        Assert.Null(move.IntermediatePoints);
    }

    [Fact]
    public void IsCombined_SingleDieMove_ReturnsFalse()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Assert
        Assert.False(move.IsCombined);
    }

    [Fact]
    public void IsCombined_CombinedMove_ReturnsTrue()
    {
        // Arrange
        var move = new Move(from: 24, to: 17, diceUsed: new[] { 6, 1 }, intermediatePoints: null);

        // Assert
        Assert.True(move.IsCombined);
    }

    [Fact]
    public void IsCombined_SingleDiceUsed_ReturnsFalse()
    {
        // Arrange - DiceUsed with single value (shouldn't happen normally but testing edge case)
        var move = new Move(from: 24, to: 18, diceUsed: new[] { 6 }, intermediatePoints: null);

        // Assert
        Assert.False(move.IsCombined);
    }

    [Fact]
    public void IsBearOff_ToZero_ReturnsTrue()
    {
        // Arrange
        var move = new Move(from: 3, to: 0, dieValue: 3);

        // Assert
        Assert.True(move.IsBearOff);
    }

    [Fact]
    public void IsBearOff_To25_ReturnsTrue()
    {
        // Arrange
        var move = new Move(from: 22, to: 25, dieValue: 3);

        // Assert
        Assert.True(move.IsBearOff);
    }

    [Fact]
    public void IsBearOff_NormalMove_ReturnsFalse()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Assert
        Assert.False(move.IsBearOff);
    }

    [Fact]
    public void ToString_NormalMove_FormatsCorrectly()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("13 -> 7", result);
    }

    [Fact]
    public void ToString_NormalMoveWithHit_IncludesHitSuffix()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6, isHit: true);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("13 -> 7 (hit)", result);
    }

    [Fact]
    public void ToString_FromBar_FormatsCorrectly()
    {
        // Arrange
        var move = new Move(from: 0, to: 20, dieValue: 5);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("Bar -> 20", result);
    }

    [Fact]
    public void ToString_FromBarWithHit_IncludesHitSuffix()
    {
        // Arrange
        var move = new Move(from: 0, to: 20, dieValue: 5, isHit: true);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("Bar -> 20 (hit)", result);
    }

    [Fact]
    public void ToString_BearOff_FormatsCorrectly()
    {
        // Arrange
        var move = new Move(from: 3, to: 0, dieValue: 3);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("3 -> Off", result);
    }

    [Fact]
    public void ToString_BearOffTo25_FormatsCorrectly()
    {
        // Arrange
        var move = new Move(from: 22, to: 25, dieValue: 3);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("22 -> Off", result);
    }

    [Fact]
    public void ToString_CombinedMove_IncludesDiceSuffix()
    {
        // Arrange
        var move = new Move(from: 24, to: 17, diceUsed: new[] { 6, 1 }, intermediatePoints: new[] { 18 });

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("24 -> 17 [dice: 6+1]", result);
    }

    [Fact]
    public void ToString_CombinedMoveWithHit_IncludesBothSuffixes()
    {
        // Arrange
        var move = new Move(from: 24, to: 17, diceUsed: new[] { 6, 1 }, intermediatePoints: new[] { 18 }, isHit: true);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("24 -> 17 (hit) [dice: 6+1]", result);
    }

    [Fact]
    public void ToString_CombinedBearOff_FormatsCorrectly()
    {
        // Arrange
        var move = new Move(from: 6, to: 0, diceUsed: new[] { 3, 3 }, intermediatePoints: new[] { 3 });

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("6 -> Off [dice: 3+3]", result);
    }

    [Fact]
    public void ToString_FromBarCombined_FormatsCorrectly()
    {
        // Arrange - This is an unusual case but tests the logic
        var move = new Move(from: 0, to: 18, diceUsed: new[] { 3, 2 }, intermediatePoints: new[] { 22 }, isHit: true);

        // Act
        var result = move.ToString();

        // Assert
        Assert.Equal("Bar -> 18 (hit) [dice: 3+2]", result);
    }

    [Fact]
    public void OpponentCheckersOnBarBefore_CanBeSet()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Act
        move.OpponentCheckersOnBarBefore = 2;

        // Assert
        Assert.Equal(2, move.OpponentCheckersOnBarBefore);
    }

    [Fact]
    public void CurrentPlayerBornOffBefore_CanBeSet()
    {
        // Arrange
        var move = new Move(from: 3, to: 0, dieValue: 3);

        // Act
        move.CurrentPlayerBornOffBefore = 10;

        // Assert
        Assert.Equal(10, move.CurrentPlayerBornOffBefore);
    }

    [Fact]
    public void Properties_CanBeModified()
    {
        // Arrange
        var move = new Move(from: 13, to: 7, dieValue: 6);

        // Act
        move.From = 24;
        move.To = 18;
        move.DieValue = 6;
        move.IsHit = true;
        move.DiceUsed = new[] { 6 };
        move.IntermediatePoints = new[] { 20 };

        // Assert
        Assert.Equal(24, move.From);
        Assert.Equal(18, move.To);
        Assert.Equal(6, move.DieValue);
        Assert.True(move.IsHit);
        Assert.NotNull(move.DiceUsed);
        Assert.NotNull(move.IntermediatePoints);
    }
}
