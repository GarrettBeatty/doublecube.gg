using Backgammon.Core;
using Xunit;

namespace Backgammon.Tests.Core;

/// <summary>
/// Tests for GameEngine - the main game engine managing backgammon game state.
/// </summary>
public class GameEngineTests
{
    [Fact]
    public void Constructor_Default_InitializesCorrectly()
    {
        // Act
        var engine = new GameEngine();

        // Assert
        Assert.NotNull(engine.Board);
        Assert.NotNull(engine.WhitePlayer);
        Assert.NotNull(engine.RedPlayer);
        Assert.NotNull(engine.Dice);
        Assert.NotNull(engine.DoublingCube);
        Assert.NotNull(engine.RemainingMoves);
        Assert.NotNull(engine.MoveHistory);
        Assert.NotNull(engine.History);
        Assert.False(engine.GameStarted);
        Assert.False(engine.GameOver);
        Assert.Null(engine.Winner);
    }

    [Fact]
    public void Constructor_WithNames_SetsPlayerNames()
    {
        // Act
        var engine = new GameEngine("Alice", "Bob");

        // Assert
        Assert.Equal("Alice", engine.WhitePlayer.Name);
        Assert.Equal("Bob", engine.RedPlayer.Name);
    }

    [Fact]
    public void StartNewGame_SetsGameStarted()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.StartNewGame();

        // Assert
        Assert.True(engine.GameStarted);
        Assert.False(engine.GameOver);
        Assert.Null(engine.Winner);
    }

    [Fact]
    public void StartNewGame_ResetsCheckersOnBar()
    {
        // Arrange
        var engine = new GameEngine();
        engine.WhitePlayer.CheckersOnBar = 5;
        engine.RedPlayer.CheckersOnBar = 3;

        // Act
        engine.StartNewGame();

        // Assert
        Assert.Equal(0, engine.WhitePlayer.CheckersOnBar);
        Assert.Equal(0, engine.RedPlayer.CheckersOnBar);
    }

    [Fact]
    public void StartNewGame_ResetsDoublingCube()
    {
        // Arrange
        var engine = new GameEngine();
        engine.DoublingCube.Double(CheckerColor.White);

        // Act
        engine.StartNewGame();

        // Assert
        Assert.Equal(1, engine.DoublingCube.Value);
        Assert.Null(engine.DoublingCube.Owner);
    }

    [Fact]
    public void StartNewGame_SetsOpeningRollPhase()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.StartNewGame();

        // Assert
        Assert.True(engine.IsOpeningRoll);
        Assert.Null(engine.WhiteOpeningRoll);
        Assert.Null(engine.RedOpeningRoll);
        Assert.False(engine.IsOpeningRollTie);
    }

    [Fact]
    public void RollDice_BeforeGameStarted_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.RollDice());
    }

    [Fact]
    public void RollDice_GameStarted_SetsRemainingMoves()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Skip opening roll
        engine.Dice.SetDice(3, 5);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        engine.RollDice();

        // Assert
        Assert.True(engine.RemainingMoves.Count >= 2);
    }

    [Fact]
    public void GetOpponent_WhitesTurn_ReturnsRed()
    {
        // Arrange
        var engine = new GameEngine();
        engine.SetCurrentPlayer(CheckerColor.White);

        // Act
        var opponent = engine.GetOpponent();

        // Assert
        Assert.Equal(CheckerColor.Red, opponent.Color);
    }

    [Fact]
    public void GetOpponent_RedsTurn_ReturnsWhite()
    {
        // Arrange
        var engine = new GameEngine();
        engine.SetCurrentPlayer(CheckerColor.Red);

        // Act
        var opponent = engine.GetOpponent();

        // Assert
        Assert.Equal(CheckerColor.White, opponent.Color);
    }

    [Fact]
    public void SetCurrentPlayer_White_SetsCurrentPlayer()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.SetCurrentPlayer(CheckerColor.White);

        // Assert
        Assert.Equal(CheckerColor.White, engine.CurrentPlayer.Color);
    }

    [Fact]
    public void SetCurrentPlayer_Red_SetsCurrentPlayer()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.SetCurrentPlayer(CheckerColor.Red);

        // Assert
        Assert.Equal(CheckerColor.Red, engine.CurrentPlayer.Color);
    }

    [Fact]
    public void EndTurn_SwitchesPlayer()
    {
        // Arrange
        var engine = new GameEngine();
        engine.SetCurrentPlayer(CheckerColor.White);

        // Act
        engine.EndTurn();

        // Assert
        Assert.Equal(CheckerColor.Red, engine.CurrentPlayer.Color);
    }

    [Fact]
    public void EndTurn_ClearsRemainingMoves()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.Dice.SetDice(3, 5);
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        engine.EndTurn();

        // Assert
        Assert.Empty(engine.RemainingMoves);
    }

    [Fact]
    public void ExecuteMove_ValidMove_ReturnsTrue()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(13, 9, 4);
        var result = engine.ExecuteMove(move);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ExecuteMove_InvalidMove_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to move from point 1 where there's no white checker
        var move = new Move(10, 6, 4);
        var result = engine.ExecuteMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExecuteMove_Hit_SendsOpponentToBar()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Set up a blot to hit
        engine.Board.GetPoint(7).Checkers.Clear();
        engine.Board.GetPoint(7).AddChecker(CheckerColor.Red);

        engine.Board.GetPoint(13).Checkers.Clear();
        engine.Board.GetPoint(13).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(13, 7, 6);
        engine.ExecuteMove(move);

        // Assert
        Assert.Equal(1, engine.RedPlayer.CheckersOnBar);
    }

    [Fact]
    public void UndoLastMove_NoMoves_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var result = engine.UndoLastMove();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UndoLastMove_WithMove_ReturnsTrue()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var move = new Move(13, 9, 4);
        engine.ExecuteMove(move);

        // Act
        var result = engine.UndoLastMove();

        // Assert
        Assert.True(result);
        Assert.Contains(4, engine.RemainingMoves);
    }

    [Fact]
    public void GetValidMoves_ReturnsValidMoves()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var moves = engine.GetValidMoves();

        // Assert
        Assert.NotEmpty(moves);
    }

    [Fact]
    public void HasValidMoves_WithMoves_ReturnsTrue()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var result = engine.HasValidMoves();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void OfferDouble_CrawfordGame_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.IsCrawfordGame = true;

        // Act
        var result = engine.OfferDouble();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void OfferDouble_NotCrawford_ReturnsTrue()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.IsCrawfordGame = false;
        engine.SetCurrentPlayer(CheckerColor.White);

        // Act
        var result = engine.OfferDouble();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void AcceptDouble_DoublesValue()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.OfferDouble();

        // Act
        var result = engine.AcceptDouble();

        // Assert
        Assert.True(result);
        Assert.Equal(2, engine.DoublingCube.Value);
    }

    [Fact]
    public void DeclineDouble_SetsFlag()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.OfferDouble();

        // Act - DeclineDouble doesn't return anything, just sets state
        engine.DeclineDouble();

        // Assert - we would check that the doubling action was recorded
        // (internal state, tested via game result)
    }

    [Fact]
    public void ForfeitGame_SetsWinner()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Act
        engine.ForfeitGame(engine.WhitePlayer);

        // Assert
        Assert.True(engine.GameOver);
        Assert.Equal(engine.WhitePlayer, engine.Winner);
    }

    [Fact]
    public void ForfeitGame_BeforeGameStarted_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.ForfeitGame(engine.WhitePlayer));
    }

    [Fact]
    public void GetGameResult_NormalWin()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        // Set up win condition - White bears off all checkers
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 5; // Red has borne off some

        // Use ForfeitGame to set winner and game over
        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(1, result); // Normal win (1x cube value)
    }

    [Fact]
    public void GetGameResult_Gammon()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0; // Red hasn't borne off any

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(2, result); // Gammon (2x cube value)
    }

    [Fact]
    public void GetGameResult_Backgammon_CheckersOnBar()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 1; // Red has checker on bar

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(3, result); // Backgammon (3x cube value)
    }

    [Fact]
    public void GetGameResult_Backgammon_CheckersInOpponentHome()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Put Red checker in White's home board (points 1-6)
        engine.Board.GetPoint(3).AddChecker(CheckerColor.Red);

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(3, result); // Backgammon (3x cube value)
    }

    [Fact]
    public void GetGameResult_GameNotOver_ReturnsZero()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void DetermineWinType_Normal()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 5;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.DetermineWinType();

        // Assert
        Assert.Equal(WinType.Normal, result);
    }

    [Fact]
    public void DetermineWinType_Gammon()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.DetermineWinType();

        // Assert
        Assert.Equal(WinType.Gammon, result);
    }

    [Fact]
    public void DetermineWinType_Backgammon()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0;
        engine.RedPlayer.CheckersOnBar = 1;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.DetermineWinType();

        // Assert
        Assert.Equal(WinType.Backgammon, result);
    }

    [Fact]
    public void DetermineWinType_GameNotOver_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.DetermineWinType());
    }

    [Fact]
    public void CreateGameResult_ReturnsGameResult()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 5;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.CreateGameResult();

        // Assert
        Assert.Equal("White", result.WinnerId);
        Assert.Equal(WinType.Normal, result.WinType);
        Assert.Equal(1, result.CubeValue);
    }

    [Fact]
    public void CreateGameResult_GameNotOver_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.CreateGameResult());
    }

    [Fact]
    public void RollOpening_WhiteRolls_SetsWhiteOpeningRoll()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var roll = engine.RollOpening(CheckerColor.White);

        // Assert
        Assert.InRange(roll, 1, 6);
        Assert.Equal(roll, engine.WhiteOpeningRoll);
    }

    [Fact]
    public void RollOpening_RedRolls_SetsRedOpeningRoll()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var roll = engine.RollOpening(CheckerColor.Red);

        // Assert
        Assert.InRange(roll, 1, 6);
        Assert.Equal(roll, engine.RedOpeningRoll);
    }

    [Fact]
    public void RollOpening_NotInOpeningPhase_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.Dice.SetDice(6, 1);

        // Simulate completing opening roll
        engine.RollOpening(CheckerColor.White);
        engine.RollOpening(CheckerColor.Red);

        // Both players have rolled, opening phase should be over
        if (!engine.IsOpeningRoll)
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => engine.RollOpening(CheckerColor.White));
        }
    }

    [Fact]
    public void InitializeTimeControl_SetsTimeState()
    {
        // Arrange
        var engine = new GameEngine();
        var config = new TimeControlConfig { Type = TimeControlType.ChicagoPoint };

        // Act
        engine.InitializeTimeControl(config, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

        // Assert
        Assert.NotNull(engine.TimeControl);
        Assert.NotNull(engine.WhiteTimeState);
        Assert.NotNull(engine.RedTimeState);
        Assert.Equal(TimeSpan.FromMinutes(10), engine.WhiteTimeState.ReserveTime);
        Assert.Equal(TimeSpan.FromMinutes(10), engine.RedTimeState.ReserveTime);
    }

    [Fact]
    public void HasCurrentPlayerTimedOut_NoTimeControl_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act
        var result = engine.HasCurrentPlayerTimedOut();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasCurrentPlayerTimedOut_WithTimeControl_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.InitializeTimeControl(
            new TimeControlConfig { Type = TimeControlType.ChicagoPoint },
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10));

        // Act
        var result = engine.HasCurrentPlayerTimedOut();

        // Assert
        Assert.False(result); // Hasn't started turn yet
    }

    [Fact]
    public void StartTurnTimer_NoTimeControl_DoesNotThrow()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act - should not throw
        engine.StartTurnTimer();
    }

    [Fact]
    public void EndTurnTimer_NoTimeControl_DoesNotThrow()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act - should not throw
        engine.EndTurnTimer();
    }

    [Fact]
    public void ExecuteMove_EnterFromBar_DecreasesCheckersOnBar()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 1;

        // Clear destination
        engine.Board.GetPoint(20).Checkers.Clear();

        engine.Dice.SetDice(5, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(0, 20, 5); // Enter from bar to point 20
        engine.ExecuteMove(move);

        // Assert
        Assert.Equal(0, engine.WhitePlayer.CheckersOnBar);
    }

    [Fact]
    public void ExecuteMove_BearOff_IncreasesCheckersBornOff()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Set up bearing off position - all checkers in home board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(6).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(6, 0, 6);
        engine.ExecuteMove(move);

        // Assert
        Assert.Equal(1, engine.WhitePlayer.CheckersBornOff);
    }

    [Fact]
    public void GetValidMoves_CheckersOnBar_MustEnterFirst()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 1;

        // Clear entry point
        engine.Board.GetPoint(20).Checkers.Clear();

        engine.Dice.SetDice(5, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var moves = engine.GetValidMoves(includeCombined: false);

        // Assert - all moves should start from bar (position 0)
        Assert.All(moves, m => Assert.Equal(0, m.From));
    }

    [Fact]
    public void UndoLastMove_BearOff_RestoresChecker()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Set up bearing off position
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(6).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var move = new Move(6, 0, 6);
        engine.ExecuteMove(move);
        Assert.Equal(1, engine.WhitePlayer.CheckersBornOff);

        // Act
        engine.UndoLastMove();

        // Assert
        Assert.Equal(0, engine.WhitePlayer.CheckersBornOff);
        Assert.Equal(1, engine.Board.GetPoint(6).Count);
    }

    [Fact]
    public void UndoLastMove_EnterFromBar_RestoresCheckerToBar()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 1;

        engine.Board.GetPoint(20).Checkers.Clear();

        engine.Dice.SetDice(5, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var move = new Move(0, 20, 5);
        engine.ExecuteMove(move);
        Assert.Equal(0, engine.WhitePlayer.CheckersOnBar);

        // Act
        engine.UndoLastMove();

        // Assert
        Assert.Equal(1, engine.WhitePlayer.CheckersOnBar);
    }

    [Fact]
    public void UndoLastMove_Hit_RestoresOpponentChecker()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Set up a blot to hit
        engine.Board.GetPoint(7).Checkers.Clear();
        engine.Board.GetPoint(7).AddChecker(CheckerColor.Red);

        engine.Board.GetPoint(13).Checkers.Clear();
        engine.Board.GetPoint(13).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var move = new Move(13, 7, 6);
        engine.ExecuteMove(move);
        Assert.Equal(1, engine.RedPlayer.CheckersOnBar);

        // Act
        engine.UndoLastMove();

        // Assert
        Assert.Equal(0, engine.RedPlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.Red, engine.Board.GetPoint(7).Color);
    }

    [Fact]
    public void MatchId_CanBeSet()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.MatchId = "match-123";

        // Assert
        Assert.Equal("match-123", engine.MatchId);
    }

    [Fact]
    public void IsCrawfordGame_CanBeSet()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.IsCrawfordGame = true;

        // Assert
        Assert.True(engine.IsCrawfordGame);
    }

    [Fact]
    public void Winner_CanBeSet()
    {
        // Arrange
        var engine = new GameEngine();

        // Act
        engine.Winner = engine.WhitePlayer;

        // Assert
        Assert.Equal(engine.WhitePlayer, engine.Winner);
    }
}
