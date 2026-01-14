using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for DoublingCube - the doubling cube in backgammon.
/// </summary>
public class DoublingCubeTests
{
    [Fact]
    public void Constructor_InitialValue_IsOne()
    {
        // Act
        var cube = new DoublingCube();

        // Assert
        Assert.Equal(1, cube.Value);
    }

    [Fact]
    public void Constructor_InitialOwner_IsNull()
    {
        // Act
        var cube = new DoublingCube();

        // Assert
        Assert.Null(cube.Owner);
    }

    [Fact]
    public void CanDouble_InitialState_BothPlayersCanDouble()
    {
        // Arrange
        var cube = new DoublingCube();

        // Assert - when cube is in middle (owner null), either player can double
        Assert.True(cube.CanDouble(CheckerColor.White));
        Assert.True(cube.CanDouble(CheckerColor.Red));
    }

    [Fact]
    public void CanDouble_OwnerIsWhite_OnlyWhiteCanDouble()
    {
        // Arrange
        var cube = new DoublingCube();
        cube.Double(CheckerColor.White);

        // Assert
        Assert.True(cube.CanDouble(CheckerColor.White));
        Assert.False(cube.CanDouble(CheckerColor.Red));
    }

    [Fact]
    public void CanDouble_OwnerIsRed_OnlyRedCanDouble()
    {
        // Arrange
        var cube = new DoublingCube();
        cube.Double(CheckerColor.Red);

        // Assert
        Assert.False(cube.CanDouble(CheckerColor.White));
        Assert.True(cube.CanDouble(CheckerColor.Red));
    }

    [Fact]
    public void CanDouble_AtMaxValue_ReturnsFalse()
    {
        // Arrange
        var cube = new DoublingCube();

        // Double to max value (64)
        cube.Double(CheckerColor.White); // 2
        cube.Double(CheckerColor.White); // 4
        cube.Double(CheckerColor.White); // 8
        cube.Double(CheckerColor.White); // 16
        cube.Double(CheckerColor.White); // 32
        cube.Double(CheckerColor.White); // 64

        // Assert
        Assert.False(cube.CanDouble(CheckerColor.White));
        Assert.False(cube.CanDouble(CheckerColor.Red));
    }

    [Fact]
    public void Double_Success_DoublesValue()
    {
        // Arrange
        var cube = new DoublingCube();

        // Act
        var result = cube.Double(CheckerColor.White);

        // Assert
        Assert.True(result);
        Assert.Equal(2, cube.Value);
    }

    [Fact]
    public void Double_Success_SetsOwner()
    {
        // Arrange
        var cube = new DoublingCube();

        // Act
        cube.Double(CheckerColor.White);

        // Assert
        Assert.Equal(CheckerColor.White, cube.Owner);
    }

    [Fact]
    public void Double_MultipleTimes_DoublesCorrectly()
    {
        // Arrange
        var cube = new DoublingCube();

        // Act
        cube.Double(CheckerColor.White); // 2
        cube.Double(CheckerColor.White); // 4
        cube.Double(CheckerColor.White); // 8

        // Assert
        Assert.Equal(8, cube.Value);
    }

    [Fact]
    public void Double_AtMaxValue_ReturnsFalse()
    {
        // Arrange
        var cube = new DoublingCube();

        // Double to max
        cube.Double(CheckerColor.White); // 2
        cube.Double(CheckerColor.White); // 4
        cube.Double(CheckerColor.White); // 8
        cube.Double(CheckerColor.White); // 16
        cube.Double(CheckerColor.White); // 32
        cube.Double(CheckerColor.White); // 64

        // Act
        var result = cube.Double(CheckerColor.White);

        // Assert
        Assert.False(result);
        Assert.Equal(64, cube.Value);
    }

    [Fact]
    public void Double_AlternatingPlayers_UpdatesOwner()
    {
        // Arrange
        var cube = new DoublingCube();

        // Act
        cube.Double(CheckerColor.White); // White owns, value 2
        cube.Double(CheckerColor.Red);   // Red owns, value 4

        // Assert
        Assert.Equal(4, cube.Value);
        Assert.Equal(CheckerColor.Red, cube.Owner);
    }

    [Fact]
    public void Reset_RestoresInitialState()
    {
        // Arrange
        var cube = new DoublingCube();
        cube.Double(CheckerColor.White);
        cube.Double(CheckerColor.Red);

        // Act
        cube.Reset();

        // Assert
        Assert.Equal(1, cube.Value);
        Assert.Null(cube.Owner);
    }

    [Fact]
    public void MaxCubeValue_Is64()
    {
        // Assert
        Assert.Equal(64, DoublingCube.MaxCubeValue);
    }

    [Fact]
    public void Double_Sequence_1_2_4_8_16_32_64()
    {
        // Arrange
        var cube = new DoublingCube();
        var expectedValues = new[] { 2, 4, 8, 16, 32, 64 };

        // Act & Assert
        foreach (var expected in expectedValues)
        {
            var result = cube.Double(CheckerColor.White);
            Assert.True(result);
            Assert.Equal(expected, cube.Value);
        }

        // 7th double should fail
        Assert.False(cube.Double(CheckerColor.White));
    }
}
