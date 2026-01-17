using Backgammon.Core;
using Backgammon.Server.Models;
using Xunit;

namespace Backgammon.Tests.Models;

/// <summary>
/// Tests for TurnSnapshotDto - data transfer object for turn snapshots.
/// </summary>
public class TurnSnapshotDtoTests
{
    // ==================== FromCore() Tests ====================

    [Fact]
    public void FromCore_BasicTurn_MapsAllProperties()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 5,
            Player = CheckerColor.White,
            DiceRolled = new[] { 3, 5 },
            PositionSgf = "(;GM[6])",
            CubeValue = 2,
            CubeOwner = "White"
        };
        turn.Moves.Add(new Move(24, 21, 3));
        turn.Moves.Add(new Move(13, 8, 5));

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal(5, dto.TurnNumber);
        Assert.Equal("White", dto.Player);
        Assert.Equal(new[] { 3, 5 }, dto.DiceRolled);
        Assert.Equal("(;GM[6])", dto.PositionSgf);
        Assert.Equal(2, dto.CubeValue);
        Assert.Equal("White", dto.CubeOwner);
    }

    [Fact]
    public void FromCore_WithMoves_ConvertsToNotation()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 1,
            Player = CheckerColor.White,
            DiceRolled = new[] { 6, 4 }
        };
        turn.Moves.Add(new Move(24, 18, 6)); // Normal move
        turn.Moves.Add(new Move(13, 9, 4));  // Normal move

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal(2, dto.Moves.Count);
        Assert.Equal("24/18", dto.Moves[0]);
        Assert.Equal("13/9", dto.Moves[1]);
    }

    [Fact]
    public void FromCore_WithBarEntry_ConvertsToBarNotation()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 3,
            Player = CheckerColor.White,
            DiceRolled = new[] { 5, 3 }
        };
        turn.Moves.Add(new Move(0, 20, 5)); // Bar entry

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Single(dto.Moves);
        Assert.Equal("bar/20", dto.Moves[0]);
    }

    [Fact]
    public void FromCore_WithBearOff_ConvertsToOffNotation()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 10,
            Player = CheckerColor.White,
            DiceRolled = new[] { 6, 5 }
        };
        turn.Moves.Add(new Move(6, 0, 6)); // Bear off

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Single(dto.Moves);
        Assert.Equal("6/off", dto.Moves[0]);
    }

    [Fact]
    public void FromCore_WithDoublingAction_MapsCorrectly()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 4,
            Player = CheckerColor.Red,
            DoublingAction = DoublingAction.Offered
        };

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal("Offered", dto.DoublingAction);
    }

    [Fact]
    public void FromCore_WithAcceptedDouble_MapsCorrectly()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 5,
            Player = CheckerColor.White,
            DoublingAction = DoublingAction.Accepted
        };

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal("Accepted", dto.DoublingAction);
    }

    [Fact]
    public void FromCore_WithDeclinedDouble_MapsCorrectly()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 6,
            Player = CheckerColor.Red,
            DoublingAction = DoublingAction.Declined
        };

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal("Declined", dto.DoublingAction);
    }

    [Fact]
    public void FromCore_NoDoublingAction_ReturnsNull()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 1,
            Player = CheckerColor.White
        };

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Null(dto.DoublingAction);
    }

    [Fact]
    public void FromCore_DoublesDice_PreservesFourValues()
    {
        // Arrange
        var turn = new TurnSnapshot
        {
            TurnNumber = 2,
            Player = CheckerColor.White,
            DiceRolled = new[] { 6, 6, 6, 6 }
        };

        // Act
        var dto = TurnSnapshotDto.FromCore(turn);

        // Assert
        Assert.Equal(4, dto.DiceRolled.Length);
        Assert.All(dto.DiceRolled, d => Assert.Equal(6, d));
    }

    // ==================== ToCore() Tests ====================

    [Fact]
    public void ToCore_BasicDto_MapsAllProperties()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 3,
            Player = "Red",
            DiceRolled = new[] { 4, 2 },
            PositionSgf = "(;GM[6]AW[a])",
            CubeValue = 4,
            CubeOwner = "Red"
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Equal(3, turn.TurnNumber);
        Assert.Equal(CheckerColor.Red, turn.Player);
        Assert.Equal(new[] { 4, 2 }, turn.DiceRolled);
        Assert.Equal("(;GM[6]AW[a])", turn.PositionSgf);
        Assert.Equal(4, turn.CubeValue);
        Assert.Equal("Red", turn.CubeOwner);
    }

    [Fact]
    public void ToCore_WithMoveNotations_ParsesCorrectly()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 1,
            Player = "White",
            DiceRolled = new[] { 6, 4 },
            Moves = new List<string> { "24/18", "13/9" }
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Equal(2, turn.Moves.Count);
        Assert.Equal(24, turn.Moves[0].From);
        Assert.Equal(18, turn.Moves[0].To);
        Assert.Equal(13, turn.Moves[1].From);
        Assert.Equal(9, turn.Moves[1].To);
    }

    [Fact]
    public void ToCore_WithBarNotation_ParsesCorrectly()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 5,
            Player = "White",
            DiceRolled = new[] { 5, 3 },
            Moves = new List<string> { "bar/20" }
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Single(turn.Moves);
        Assert.Equal(0, turn.Moves[0].From);
        Assert.Equal(20, turn.Moves[0].To);
    }

    [Fact]
    public void ToCore_WithBearOffNotation_ParsesCorrectly()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 10,
            Player = "White",
            DiceRolled = new[] { 6, 1 },
            Moves = new List<string> { "6/off" }
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Single(turn.Moves);
        Assert.Equal(6, turn.Moves[0].From);
        Assert.Equal(0, turn.Moves[0].To); // White bears off to 0
    }

    [Fact]
    public void ToCore_RedBearOff_ParsesCorrectly()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 12,
            Player = "Red",
            DiceRolled = new[] { 6, 1 },
            Moves = new List<string> { "19/off" }
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Single(turn.Moves);
        Assert.Equal(19, turn.Moves[0].From);
        Assert.Equal(25, turn.Moves[0].To); // Red bears off to 25
    }

    [Fact]
    public void ToCore_WithDoublingAction_ParsesCorrectly()
    {
        // Arrange
        var dto = new TurnSnapshotDto
        {
            TurnNumber = 4,
            Player = "Red",
            DoublingAction = "Offered"
        };

        // Act
        var turn = dto.ToCore();

        // Assert
        Assert.Equal(DoublingAction.Offered, turn.DoublingAction);
    }

    // ==================== Round-Trip Tests ====================

    [Fact]
    public void RoundTrip_ComplexTurn_PreservesData()
    {
        // Arrange
        var original = new TurnSnapshot
        {
            TurnNumber = 7,
            Player = CheckerColor.White,
            DiceRolled = new[] { 6, 3 },
            PositionSgf = "(;GM[6]AW[a][b]AB[x][y])",
            CubeValue = 2,
            CubeOwner = "Red",
            DoublingAction = DoublingAction.Accepted
        };
        original.Moves.Add(new Move(24, 18, 6));
        original.Moves.Add(new Move(18, 15, 3));

        // Act
        var dto = TurnSnapshotDto.FromCore(original);
        var restored = dto.ToCore();

        // Assert
        Assert.Equal(original.TurnNumber, restored.TurnNumber);
        Assert.Equal(original.Player, restored.Player);
        Assert.Equal(original.DiceRolled, restored.DiceRolled);
        Assert.Equal(original.PositionSgf, restored.PositionSgf);
        Assert.Equal(original.CubeValue, restored.CubeValue);
        Assert.Equal(original.CubeOwner, restored.CubeOwner);
        Assert.Equal(original.DoublingAction, restored.DoublingAction);
        Assert.Equal(original.Moves.Count, restored.Moves.Count);
    }

    [Fact]
    public void RoundTrip_WithBarAndBearOff_PreservesData()
    {
        // Arrange
        var original = new TurnSnapshot
        {
            TurnNumber = 15,
            Player = CheckerColor.White,
            DiceRolled = new[] { 5, 6 }
        };
        original.Moves.Add(new Move(0, 20, 5));  // Bar entry
        original.Moves.Add(new Move(6, 0, 6));  // Bear off

        // Act
        var dto = TurnSnapshotDto.FromCore(original);
        var restored = dto.ToCore();

        // Assert
        Assert.Equal(2, restored.Moves.Count);
        Assert.Equal(0, restored.Moves[0].From);
        Assert.Equal(20, restored.Moves[0].To);
        Assert.Equal(6, restored.Moves[1].From);
        Assert.Equal(0, restored.Moves[1].To);
    }
}
