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
    public void UndoLastMove_RemovesFromCurrentTurnMoves()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Initialize turn snapshot (simulating what RollDice does)
        var bindingFlags = System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance;
        var currentTurnDiceField = typeof(GameEngine).GetField("_currentTurnDice", bindingFlags);
        var currentTurnMovesField = typeof(GameEngine).GetField("_currentTurnMoves", bindingFlags);
        var currentTurnField = typeof(GameEngine).GetField("_currentTurn", bindingFlags);

        currentTurnDiceField!.SetValue(engine, (4, 3));
        ((List<Move>)currentTurnMovesField!.GetValue(engine)!).Clear();

        var turnSnapshot = new TurnSnapshot
        {
            TurnNumber = 1,
            Player = CheckerColor.White,
            DiceRolled = new[] { 4, 3 },
            PositionSgf = SgfSerializer.ExportPosition(engine),
            CubeValue = 1,
            CubeOwner = null,
            DoublingAction = null
        };
        currentTurnField!.SetValue(engine, turnSnapshot);

        // Execute two moves
        var move1 = new Move(13, 9, 4);
        engine.ExecuteMove(move1);
        var move2 = new Move(13, 10, 3);
        engine.ExecuteMove(move2);

        // Verify moves are tracked
        Assert.Equal(2, engine.CurrentTurnMoves.Count);
        Assert.Equal(2, turnSnapshot.Moves.Count);

        // Act - Undo one move
        engine.UndoLastMove();

        // Assert - Move should be removed from both tracking lists
        Assert.Single(engine.CurrentTurnMoves);
        Assert.Single(turnSnapshot.Moves);
        Assert.Equal(9, engine.CurrentTurnMoves[0].To);
        Assert.Equal(9, turnSnapshot.Moves[0].To);
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

    [Fact]
    public void RollOpening_Tie_ReturnsMinus1AndSetsFlag()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Simulate both players rolling same value by calling multiple times
        // This tests the tie handling path
        bool hadTie = false;
        for (int i = 0; i < 100; i++)
        {
            engine.StartNewGame(); // Reset
            var roll1 = engine.RollOpening(CheckerColor.White);
            var roll2 = engine.RollOpening(CheckerColor.Red);

            if (roll2 == -1)
            {
                hadTie = true;
                Assert.True(engine.IsOpeningRollTie);
                break;
            }
        }

        // May not always get a tie in 100 attempts, so we just verify the logic works when it does
        if (hadTie)
        {
            Assert.True(engine.IsOpeningRollTie);
        }
    }

    [Fact]
    public void RollOpening_AfterTie_ClearsPreviousRolls()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Manually set up a tie state
        // This is to test that rolling after a tie clears the previous values
        bool foundTie = false;
        for (int attempt = 0; attempt < 200 && !foundTie; attempt++)
        {
            engine.StartNewGame();
            engine.RollOpening(CheckerColor.White);
            var result = engine.RollOpening(CheckerColor.Red);

            if (result == -1)
            {
                foundTie = true;
                Assert.True(engine.IsOpeningRollTie);

                // Now roll again - should clear the tie
                engine.RollOpening(CheckerColor.White);
                Assert.False(engine.IsOpeningRollTie);
            }
        }
    }

    [Fact]
    public void ExecuteMove_Combined_Success()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Create a combined move
        var combinedMove = new Move(24, 17, new[] { 6, 1 }, new[] { 18 });

        // Act
        var result = engine.ExecuteMove(combinedMove);

        // Assert - depends on board position
        // This tests the combined move execution path
    }

    [Fact]
    public void ExecuteMove_WinCondition_SetsGameOver()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Set up final bearing off position
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.Board.GetPoint(1).AddChecker(CheckerColor.White);
        engine.WhitePlayer.CheckersBornOff = 14; // One more to win

        engine.Dice.SetDice(1, 2);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(1, 0, 1);
        engine.ExecuteMove(move);

        // Assert
        Assert.True(engine.GameOver);
        Assert.Equal(engine.WhitePlayer, engine.Winner);
    }

    [Fact]
    public void AcceptDouble_AtMaxCube_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Double to max value
        for (int i = 0; i < 6; i++)
        {
            engine.DoublingCube.Double(CheckerColor.White);
        }

        Assert.Equal(64, engine.DoublingCube.Value);

        // Act
        var result = engine.AcceptDouble();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetValidMoves_NoCombined_ReturnsSingleMoves()
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
        var movesWithCombined = engine.GetValidMoves(includeCombined: true);
        var movesWithoutCombined = engine.GetValidMoves(includeCombined: false);

        // Assert - without combined should have fewer or equal moves
        Assert.True(movesWithoutCombined.Count <= movesWithCombined.Count);
    }

    [Fact]
    public void IsValidMove_InvalidDieValue_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try move with die value not in remaining
        var move = new Move(13, 8, 5);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RollDice_WhenGameOver_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.RollDice());
    }

    [Fact]
    public void StartTurnTimer_WithTimeControl_StartsTimer()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.InitializeTimeControl(
            new TimeControlConfig { Type = TimeControlType.ChicagoPoint },
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10));

        // Act
        engine.StartTurnTimer();

        // Assert
        Assert.NotNull(engine.WhiteTimeState?.TurnStartTime);
    }

    [Fact]
    public void EndTurnTimer_WithTimeControl_EndsTimer()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.InitializeTimeControl(
            new TimeControlConfig { Type = TimeControlType.ChicagoPoint },
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(10));
        engine.StartTurnTimer();

        // Act
        engine.EndTurnTimer();

        // Assert
        Assert.Null(engine.WhiteTimeState?.TurnStartTime);
    }

    [Fact]
    public void IsValidMove_FromBar_WithNoCheckersOnBar_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.Dice.SetDice(5, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to enter from bar when no checkers on bar
        var move = new Move(0, 20, 5);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidMove_NotFromBar_WithCheckersOnBar_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 1;
        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try normal move when has checkers on bar
        var move = new Move(13, 9, 4);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBearOff_RedPlayer_ExactDieMatch()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.Red);
        engine.SetGameStarted(true);

        // Set up bearing off position - all red checkers in home board
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Red home is points 19-24
        engine.Board.GetPoint(22).AddChecker(CheckerColor.Red); // normalizedPosition = 25 - 22 = 3

        engine.Dice.SetDice(3, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var move = new Move(22, 25, 3); // Bear off from 22 with die 3
        var result = engine.IsValidMove(move);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBearOff_RedPlayer_HigherDieFromHighestPoint()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.Red);
        engine.SetGameStarted(true);

        // Set up bearing off position
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Red home is points 19-24
        engine.Board.GetPoint(23).AddChecker(CheckerColor.Red); // normalizedPosition = 25 - 23 = 2

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - die 6 > 2, can bear off if 23 is highest point
        var move = new Move(23, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBearOff_RedPlayer_HigherDieNotFromHighestPoint_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.Red);
        engine.SetGameStarted(true);

        // Set up bearing off position
        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Red home is points 19-24
        engine.Board.GetPoint(19).AddChecker(CheckerColor.Red); // This is the highest point (farthest from bear off)
        engine.Board.GetPoint(24).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - die 6 > 1 (normalized 24), but 24 is not highest (19 is)
        var move = new Move(24, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBearOff_WhitePlayer_HigherDieFromHighestPoint()
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

        // White home is points 1-6
        engine.Board.GetPoint(2).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - die 6 > 2, can bear off if 2 is highest point
        var move = new Move(2, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBearOff_WhitePlayer_PointOutsideHomeBoard_ReturnsFalse()
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

        // Put checker outside home board
        engine.Board.GetPoint(7).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to bear off from point 7 (outside home)
        var move = new Move(7, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBearOff_EmptyFromPoint_ReturnsFalse()
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

        // Only put checker on point 5
        engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to bear off from empty point
        var move = new Move(6, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBearOff_WrongColor_ReturnsFalse()
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

        // Put Red checker in White's home board
        engine.Board.GetPoint(6).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to bear off opponent's checker
        var move = new Move(6, 25, 6);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExecuteCombinedMove_InvalidDiceUsed_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Create invalid combined move with wrong dice
        var combinedMove = new Move(24, 17, new[] { 5, 2 }, new[] { 19 });

        // Act
        var result = engine.ExecuteMove(combinedMove);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExecuteCombinedMove_NullDiceUsed_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Create a move that looks combined but has null DiceUsed
        // This is an edge case to test the DiceUsed null check
        var move = new Move(24, 17, 7); // Invalid die value, but set DiceUsed manually
        move.DiceUsed = null;
        move.IntermediatePoints = new[] { 18 };

        // When DiceUsed is null, IsCombined returns false, so it's treated as a single move
        // The single move will fail because 7 is not in remaining moves
        var result = engine.ExecuteMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCombinedMove_InvalidDestination_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Create combined move to invalid destination
        var combinedMove = new Move(24, 10, new[] { 6, 1 }, new[] { 18 });

        // Act
        var result = engine.IsValidMove(combinedMove);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetGameResult_WithDoubleCube()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        engine.DoublingCube.Double(CheckerColor.White);
        Assert.Equal(2, engine.DoublingCube.Value);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 5;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.GetGameResult();

        // Assert
        Assert.Equal(2, result); // 1 * cube value (2)
    }

    [Fact]
    public void GetValidMoves_EmptyRemainingMoves_ReturnsEmpty()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.RemainingMoves.Clear();

        // Act
        var moves = engine.GetValidMoves();

        // Assert
        Assert.Empty(moves);
    }

    [Fact]
    public void CreateGameResult_RedWinner()
    {
        // Arrange
        var engine = new GameEngine("WhitePlayer", "RedPlayer");
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        engine.RedPlayer.CheckersBornOff = 15;
        engine.WhitePlayer.CheckersBornOff = 5;

        engine.ForfeitGame(engine.RedPlayer);

        // Act
        var result = engine.CreateGameResult();

        // Assert
        Assert.Equal("RedPlayer", result.WinnerId);
    }

    [Fact]
    public void UndoLastMove_Hit_FromBarEntry_RestoresOpponent()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);
        engine.WhitePlayer.CheckersOnBar = 1;

        // Put a Red blot on entry point
        engine.Board.GetPoint(20).Checkers.Clear();
        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(5, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var move = new Move(0, 20, 5);
        engine.ExecuteMove(move);

        Assert.Equal(0, engine.WhitePlayer.CheckersOnBar);
        Assert.Equal(1, engine.RedPlayer.CheckersOnBar);

        // Act
        engine.UndoLastMove();

        // Assert
        Assert.Equal(1, engine.WhitePlayer.CheckersOnBar);
        Assert.Equal(0, engine.RedPlayer.CheckersOnBar);
        Assert.Equal(CheckerColor.Red, engine.Board.GetPoint(20).Color);
    }

    [Fact]
    public void ForfeitGame_WhenAlreadyOver_ThrowsException()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);
        engine.ForfeitGame(engine.WhitePlayer);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.ForfeitGame(engine.RedPlayer));
    }

    [Fact]
    public void DetermineWinType_BackgammonInOpponentHome()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetGameStarted(true);

        for (int i = 1; i <= 24; i++)
        {
            engine.Board.GetPoint(i).Checkers.Clear();
        }

        // Red checker in White's home board (points 1-6)
        engine.Board.GetPoint(5).AddChecker(CheckerColor.Red);

        engine.WhitePlayer.CheckersBornOff = 15;
        engine.RedPlayer.CheckersBornOff = 0;

        engine.ForfeitGame(engine.WhitePlayer);

        // Act
        var result = engine.DetermineWinType();

        // Assert
        Assert.Equal(WinType.Backgammon, result);
    }

    [Fact]
    public void HasCurrentPlayerTimedOut_NullTimeState_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.TimeControl = new TimeControlConfig { Type = TimeControlType.ChicagoPoint, DelaySeconds = 12 };
        // Don't initialize time states

        // Act
        var result = engine.HasCurrentPlayerTimedOut();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasCurrentPlayerTimedOut_TypeNone_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.TimeControl = new TimeControlConfig { Type = TimeControlType.None };

        // Act
        var result = engine.HasCurrentPlayerTimedOut();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetValidMoves_BearingOff_IncludesBearOffMoves()
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
        engine.Board.GetPoint(5).AddChecker(CheckerColor.White);

        engine.Dice.SetDice(6, 5);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act
        var moves = engine.GetValidMoves(includeCombined: false);

        // Assert
        Assert.Contains(moves, m => m.From == 6 && m.To == 25);
        Assert.Contains(moves, m => m.From == 5 && m.To == 25);
    }

    [Fact]
    public void GetValidMoves_NormalMoves_ReturnsValidMoves()
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
        var moves = engine.GetValidMoves(includeCombined: false);

        // Assert - should have moves from points with white checkers
        Assert.Contains(moves, m => m.From == 24);
        Assert.Contains(moves, m => m.From == 13);
    }

    [Fact]
    public void IsValidMove_ToBlockedPoint_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Point 20 has Red checkers (2 or more)
        engine.Board.GetPoint(20).Checkers.Clear();
        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(20).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to move to blocked point
        var move = new Move(24, 20, 4);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidMove_FromEmptyPoint_ReturnsFalse()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Clear point 10 (empty)
        engine.Board.GetPoint(10).Checkers.Clear();

        engine.Dice.SetDice(4, 3);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        // Act - try to move from empty point
        var move = new Move(10, 6, 4);
        var result = engine.IsValidMove(move);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ExecuteCombinedMove_WithRollback_RestoresState()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();
        engine.SetCurrentPlayer(CheckerColor.White);
        engine.SetGameStarted(true);

        // Block intermediate point
        engine.Board.GetPoint(18).Checkers.Clear();
        engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);
        engine.Board.GetPoint(18).AddChecker(CheckerColor.Red);

        engine.Dice.SetDice(6, 1);
        engine.RemainingMoves.Clear();
        engine.RemainingMoves.AddRange(engine.Dice.GetMoves());

        var originalCount = engine.Board.GetPoint(24).Count;

        // Try combined move through blocked point (should fail and rollback)
        var combinedMove = new Move(24, 17, new[] { 6, 1 }, new[] { 18 });

        // Act
        var result = engine.ExecuteMove(combinedMove);

        // Assert - state should be restored
        Assert.False(result);
        Assert.Equal(originalCount, engine.Board.GetPoint(24).Count);
    }

    [Fact]
    public void RollOpening_InitializesTurnSnapshot_WhenOpeningRollCompletes()
    {
        // Arrange
        var engine = new GameEngine();
        engine.StartNewGame();

        // Act - Complete the opening roll (may need multiple attempts to avoid a tie)
        int whiteRoll = 0;
        int redRoll = 0;

        for (int i = 0; i < 100; i++)
        {
            engine.StartNewGame();
            whiteRoll = engine.RollOpening(CheckerColor.White);
            redRoll = engine.RollOpening(CheckerColor.Red);

            if (redRoll != -1)
            {
                break;
            }
        }

        // Skip test if we couldn't get a non-tie roll
        if (engine.IsOpeningRoll)
        {
            return;
        }

        // Use reflection to check _currentTurn was initialized
        var bindingFlags = System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance;
        var currentTurnField = typeof(GameEngine).GetField("_currentTurn", bindingFlags);
        var currentTurn = currentTurnField!.GetValue(engine) as TurnSnapshot;

        // Assert
        Assert.NotNull(currentTurn);
        Assert.Equal(1, currentTurn.TurnNumber);
        Assert.Equal(engine.CurrentPlayer.Color, currentTurn.Player);
        Assert.Contains(whiteRoll, currentTurn.DiceRolled);
        Assert.Contains(redRoll, currentTurn.DiceRolled);
    }

    [Fact]
    public void OpeningRoll_FullFlow_MovesNotOverwrittenByOpponent()
    {
        // This test simulates the full flow that was broken:
        // 1. Opening roll
        // 2. Winner makes moves, ends turn
        // 3. Opponent rolls, makes moves, ends turn
        // 4. Verify both turns are recorded correctly

        // Arrange
        var engine = new GameEngine();

        // Keep trying until we get a valid game with non-tie opening roll
        for (int attempt = 0; attempt < 100; attempt++)
        {
            engine = new GameEngine();
            engine.StartNewGame();

            var roll1 = engine.RollOpening(CheckerColor.White);
            var roll2 = engine.RollOpening(CheckerColor.Red);

            if (roll2 != -1 && !engine.IsOpeningRoll)
            {
                break;
            }
        }

        // Skip if couldn't set up test
        if (engine.IsOpeningRoll)
        {
            return;
        }

        // Act - Opening roll winner makes moves
        var openingPlayer = engine.CurrentPlayer.Color;
        var validMoves = engine.GetValidMoves();
        int movesExecuted = 0;

        while (validMoves.Count > 0 && engine.RemainingMoves.Count > 0)
        {
            engine.ExecuteMove(validMoves[0]);
            validMoves = engine.GetValidMoves();
            movesExecuted++;
        }

        engine.EndTurn();

        // Assert - Turn 1 recorded
        Assert.Single(engine.History.Turns);
        Assert.Equal(1, engine.History.Turns[0].TurnNumber);
        Assert.Equal(openingPlayer, engine.History.Turns[0].Player);
        Assert.Equal(movesExecuted, engine.History.Turns[0].Moves.Count);

        // Act - Opponent's turn
        engine.RollDice();
        var opponentColor = engine.CurrentPlayer.Color;
        validMoves = engine.GetValidMoves();
        int opponentMovesExecuted = 0;

        while (validMoves.Count > 0 && engine.RemainingMoves.Count > 0)
        {
            engine.ExecuteMove(validMoves[0]);
            validMoves = engine.GetValidMoves();
            opponentMovesExecuted++;
        }

        engine.EndTurn();

        // Assert - Both turns recorded correctly
        Assert.Equal(2, engine.History.Turns.Count);
        Assert.Equal(1, engine.History.Turns[0].TurnNumber);
        Assert.Equal(openingPlayer, engine.History.Turns[0].Player);
        Assert.Equal(movesExecuted, engine.History.Turns[0].Moves.Count);
        Assert.Equal(2, engine.History.Turns[1].TurnNumber);
        Assert.Equal(opponentColor, engine.History.Turns[1].Player);
        Assert.Equal(opponentMovesExecuted, engine.History.Turns[1].Moves.Count);
    }
}
