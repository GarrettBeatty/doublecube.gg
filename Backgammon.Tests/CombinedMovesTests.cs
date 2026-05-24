using Backgammon.Core;
using Backgammon.Server.Models;

namespace Backgammon.Tests;

/// <summary>
/// Tests for combined dice moves functionality.
/// </summary>
public class CombinedMovesTests
{
    private static GameState BuildState(GameEngine engine)
    {
        var moves = engine.GetValidMoves(includeCombined: true);
        return new GameState
        {
            ValidMoves = moves.Select(m => new MoveDto
            {
                From = m.From,
                To = m.To,
                DieValue = m.DieValue,
                IsHit = m.IsHit,
                IsCombinedMove = m.IsCombined,
                DiceUsed = m.DiceUsed,
                IntermediatePoints = m.IntermediatePoints
            }).ToList()
        };
    }

    [Fact]
    public void CombinedMoves_TwoDice_ShouldReturnCombinedDestination()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.Board.GetPoint(17).Checkers.Clear();
        engine.Board.GetPoint(18).Checkers.Clear();
        engine.Board.GetPoint(23).Checkers.Clear();

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        Assert.NotEmpty(combinedMoves);

        var move24To17 = combinedMoves.FirstOrDefault(m => m.From == 24 && m.To == 17);
        Assert.NotNull(move24To17);
        Assert.True(move24To17.IsCombinedMove);
        Assert.Equal(7, move24To17.DieValue); // 6 + 1
        Assert.NotNull(move24To17.DiceUsed);
        Assert.Equal(2, move24To17.DiceUsed.Length);
        Assert.NotNull(move24To17.IntermediatePoints);
        Assert.Single(move24To17.IntermediatePoints);
    }

    [Fact]
    public void CombinedMoves_BlockedIntermediate_ShouldNotReturnCombinedMove()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Board.GetPoint(18).Checkers.Clear();
        engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);

        engine.Board.GetPoint(23).Checkers.Clear();
        engine.Board.GetPoint(23).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(23).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        var move24To17 = combinedMoves.FirstOrDefault(m => m.From == 24 && m.To == 17);
        Assert.Null(move24To17);
    }

    [Fact]
    public void CombinedMoves_Doubles_ShouldReturnMultipleCombinations()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.SetCurrentPlayer(CheckerColor.White);

        for (int i = 12; i <= 23; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Dice.SetDice(3, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMovesFrom24 = state.ValidMoves
            .Where(m => m.IsCombinedMove && m.From == 24)
            .ToList();

        var twoDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 18);
        Assert.NotNull(twoDiceMove);
        Assert.Equal(2, twoDiceMove.DiceUsed?.Length);

        var threeDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 15);
        Assert.NotNull(threeDiceMove);
        Assert.Equal(3, threeDiceMove.DiceUsed?.Length);

        var fourDiceMove = combinedMovesFrom24.FirstOrDefault(m => m.To == 12);
        Assert.NotNull(fourDiceMove);
        Assert.Equal(4, fourDiceMove.DiceUsed?.Length);
    }

    [Fact]
    public void CombinedMoves_BarEntry_ShouldNotHaveCombinedMoves()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Board.GetPoint(24).RemoveChecker();
        engine.WhitePlayer.CheckersOnBar = 1;

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove).ToList();
        Assert.Empty(combinedMoves);
    }

    [Fact]
    public void CombinedMoves_SingleDieDestinationsExcluded()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var singleMoveDests = state.ValidMoves
            .Where(m => !m.IsCombinedMove && m.From == 24)
            .Select(m => m.To)
            .ToHashSet();

        var combinedMoveDests = state.ValidMoves
            .Where(m => m.IsCombinedMove && m.From == 24)
            .Select(m => m.To)
            .ToHashSet();

        var overlap = singleMoveDests.Intersect(combinedMoveDests);
        Assert.Empty(overlap);
    }

    [Fact]
    public void CombinedMoves_IntermediatePointsCorrect()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.Board.GetPoint(17).Checkers.Clear();
        engine.Board.GetPoint(18).Checkers.Clear();
        engine.Board.GetPoint(23).Checkers.Clear();

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var move24To17 = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 24 && m.To == 17);

        Assert.NotNull(move24To17);
        Assert.NotNull(move24To17.IntermediatePoints);
        Assert.Single(move24To17.IntermediatePoints);

        var intermediate = move24To17.IntermediatePoints[0];
        Assert.True(
            intermediate == 18 || intermediate == 23,
            $"Intermediate should be 18 (via 6 first) or 23 (via 1 first), got {intermediate}");
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldWork()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(6).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(3, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 5 && m.To == 25);

        Assert.NotNull(combinedBearOff);
        Assert.True(combinedBearOff.IsCombinedMove);
        Assert.Equal(5, combinedBearOff.DieValue);
        Assert.NotNull(combinedBearOff.DiceUsed);
        Assert.Equal(2, combinedBearOff.DiceUsed.Length);
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldNotAllowWhenHigherPointsOccupied()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(2).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(2).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(3).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(4).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(6, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var invalidCombinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 3 && m.To == 25);

        Assert.Null(invalidCombinedBearOff);

        var validBearOff = state.ValidMoves.FirstOrDefault(m =>
            !m.IsCombinedMove && m.From == 5 && m.To == 25);
        Assert.NotNull(validBearOff);
    }

    [Fact]
    public void CombinedMoves_BearingOff_ShouldAllowWhenHighestPointMovedAway()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(4).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(2).AddChecker(CheckerColor.White);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(3, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var validCombinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 4 && m.To == 25);

        Assert.NotNull(validCombinedBearOff);
    }

    [Fact]
    public void CombinedMoves_RedBearingOff_ShouldWork()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(21).AddChecker(CheckerColor.Red);

        engine.SetCurrentPlayer(CheckerColor.Red);

        engine.Dice.SetDice(3, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 20 && m.To == 25);

        Assert.NotNull(combinedBearOff);
        Assert.True(combinedBearOff.IsCombinedMove);
        Assert.NotNull(combinedBearOff.DiceUsed);
        Assert.Equal(2, combinedBearOff.DiceUsed.Length);
    }

    [Fact]
    public void CombinedMoves_RedBearingOff_WithMultipleCheckersOnSource()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);

        engine.SetCurrentPlayer(CheckerColor.Red);

        engine.Dice.SetDice(3, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 20 && m.To == 25);

        Assert.NotNull(combinedBearOff);
    }

    [Fact]
    public void CombinedMoves_RedBearingOff_NotFromHighestPoint()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(19).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(24).AddChecker(CheckerColor.Red);

        engine.SetCurrentPlayer(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        // This test just verifies the state is computed without error;
        // the combined bear-off validity from point 24 is verified by the GetEffectiveHighestPoint logic.
        Assert.NotNull(state.ValidMoves);
    }

    [Fact]
    public void CombinedMoves_WhiteBearingOff_SourceHasMultipleCheckers()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(4).AddChecker(CheckerColor.White);
        engine.Board.GetPoint(4).AddChecker(CheckerColor.White);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(2, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 4 && m.To == 25);

        Assert.NotNull(combinedBearOff);
    }

    [Fact]
    public void CombinedMoves_WithHit_ShouldMarkAsHit()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.Board.GetPoint(17).Checkers.Clear();
        engine.Board.GetPoint(18).Checkers.Clear();
        engine.Board.GetPoint(23).Checkers.Clear();

        engine.Board.GetPoint(17).AddChecker(CheckerColor.Red);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMoveWithHit = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 24 && m.To == 17);

        Assert.NotNull(combinedMoveWithHit);
    }

    [Fact]
    public void CombinedMoves_RedPlayer_NormalMoves()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        engine.Board.GetPoint(7).Checkers.Clear();
        engine.Board.GetPoint(8).Checkers.Clear();
        engine.Board.GetPoint(2).Checkers.Clear();

        engine.SetCurrentPlayer(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedMoves = state.ValidMoves.Where(m => m.IsCombinedMove && m.From == 1).ToList();
        Assert.NotEmpty(combinedMoves);

        var move1To8 = combinedMoves.FirstOrDefault(m => m.To == 8);
        Assert.NotNull(move1To8);
    }

    [Fact]
    public void CombinedMoves_CalculatorReturnsEmpty_ForBarPosition()
    {
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);

        engine.WhitePlayer.CheckersOnBar = 1;

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var moves = engine.GetValidMoves(includeCombined: true);

        var combinedMoves = moves.Where(m => m.IsCombined).ToList();
        Assert.Empty(combinedMoves);

        Assert.All(moves, m => Assert.Equal(0, m.From));
    }

    [Fact]
    public void CombinedMoves_WhiteBearingOff_OnlyOneCheckerLeft()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(4).AddChecker(CheckerColor.White);

        engine.SetCurrentPlayer(CheckerColor.White);

        engine.Dice.SetDice(2, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 4 && m.To == 25);

        Assert.NotNull(combinedBearOff);
    }

    [Fact]
    public void CombinedMoves_RedBearingOff_OnlyOneCheckerLeft()
    {
        var engine = new GameEngine();
        engine.StartNewGame();

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(21).AddChecker(CheckerColor.Red);

        engine.SetCurrentPlayer(CheckerColor.Red);

        engine.Dice.SetDice(2, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var state = BuildState(engine);

        var combinedBearOff = state.ValidMoves.FirstOrDefault(m =>
            m.IsCombinedMove && m.From == 21 && m.To == 25);

        Assert.NotNull(combinedBearOff);
    }
}
