using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for Point - a point (triangle) on the backgammon board.
/// </summary>
public class PointTests
{
    [Fact]
    public void Constructor_SetsPosition()
    {
        // Act
        var point = new Point(13);

        // Assert
        Assert.Equal(13, point.Position);
        Assert.Empty(point.Checkers);
    }

    [Fact]
    public void Color_EmptyPoint_ReturnsNull()
    {
        // Arrange
        var point = new Point(1);

        // Assert
        Assert.Null(point.Color);
    }

    [Fact]
    public void Color_WithChecker_ReturnsCheckerColor()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.Equal(CheckerColor.White, point.Color);
    }

    [Fact]
    public void Count_EmptyPoint_ReturnsZero()
    {
        // Arrange
        var point = new Point(1);

        // Assert
        Assert.Equal(0, point.Count);
    }

    [Fact]
    public void Count_WithCheckers_ReturnsCorrectCount()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.Equal(3, point.Count);
    }

    [Fact]
    public void IsBlot_EmptyPoint_ReturnsFalse()
    {
        // Arrange
        var point = new Point(1);

        // Assert
        Assert.False(point.IsBlot);
    }

    [Fact]
    public void IsBlot_SingleChecker_ReturnsTrue()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.True(point.IsBlot);
    }

    [Fact]
    public void IsBlot_MultipleCheckers_ReturnsFalse()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.False(point.IsBlot);
    }

    [Fact]
    public void IsOpen_EmptyPoint_ReturnsTrue()
    {
        // Arrange
        var point = new Point(1);

        // Assert
        Assert.True(point.IsOpen(CheckerColor.White));
        Assert.True(point.IsOpen(CheckerColor.Red));
    }

    [Fact]
    public void IsOpen_OwnColor_ReturnsTrue()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.True(point.IsOpen(CheckerColor.White));
    }

    [Fact]
    public void IsOpen_OpponentBlot_ReturnsTrue()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.Red);

        // Assert - can land on opponent's blot
        Assert.True(point.IsOpen(CheckerColor.White));
    }

    [Fact]
    public void IsOpen_OpponentMadePoint_ReturnsFalse()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.Red);
        point.AddChecker(CheckerColor.Red);

        // Assert - cannot land on opponent's made point
        Assert.False(point.IsOpen(CheckerColor.White));
    }

    [Fact]
    public void AddChecker_EmptyPoint_AddsChecker()
    {
        // Arrange
        var point = new Point(1);

        // Act
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.Equal(1, point.Count);
        Assert.Equal(CheckerColor.White, point.Color);
    }

    [Fact]
    public void AddChecker_SameColor_AddsChecker()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);

        // Act
        point.AddChecker(CheckerColor.White);

        // Assert
        Assert.Equal(2, point.Count);
        Assert.Equal(CheckerColor.White, point.Color);
    }

    [Fact]
    public void AddChecker_DifferentColor_ThrowsInvalidOperationException()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => point.AddChecker(CheckerColor.Red));
        Assert.Equal("Cannot add different color to point", exception.Message);
    }

    [Fact]
    public void RemoveChecker_WithCheckers_RemovesAndReturnsColor()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);

        // Act
        var color = point.RemoveChecker();

        // Assert
        Assert.Equal(CheckerColor.White, color);
        Assert.Equal(1, point.Count);
    }

    [Fact]
    public void RemoveChecker_EmptyPoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var point = new Point(1);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => point.RemoveChecker());
        Assert.Equal("No checkers to remove", exception.Message);
    }

    [Fact]
    public void RemoveChecker_LastChecker_PointBecomesEmpty()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);

        // Act
        point.RemoveChecker();

        // Assert
        Assert.Equal(0, point.Count);
        Assert.Null(point.Color);
    }

    [Fact]
    public void Checkers_DirectManipulation_Works()
    {
        // Arrange
        var point = new Point(1);

        // Act
        point.Checkers.Add(CheckerColor.Red);
        point.Checkers.Add(CheckerColor.Red);
        point.Checkers.Add(CheckerColor.Red);

        // Assert
        Assert.Equal(3, point.Count);
        Assert.Equal(CheckerColor.Red, point.Color);
    }

    [Fact]
    public void Checkers_Clear_RemovesAllCheckers()
    {
        // Arrange
        var point = new Point(1);
        point.AddChecker(CheckerColor.White);
        point.AddChecker(CheckerColor.White);

        // Act
        point.Checkers.Clear();

        // Assert
        Assert.Equal(0, point.Count);
        Assert.Null(point.Color);
    }
}
