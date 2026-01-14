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
}
