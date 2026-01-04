namespace Backgammon.Core;

/// <summary>
/// Main game engine that manages the backgammon game state and rules
/// </summary>
public class GameEngine
{
    public GameEngine(string whiteName = "White", string redName = "Red")
    {
        Board = new Board();
        WhitePlayer = new Player(CheckerColor.White, whiteName);
        RedPlayer = new Player(CheckerColor.Red, redName);
        CurrentPlayer = WhitePlayer;
        Dice = new Dice();
        DoublingCube = new DoublingCube();
        RemainingMoves = new List<int>();
        MoveHistory = new List<Move>();
        GameStarted = false;
        GameOver = false;
    }

    public Board Board { get; }

    public Player WhitePlayer { get; }

    public Player RedPlayer { get; }

    public Player CurrentPlayer { get; private set; }

    public Dice Dice { get; }

    public DoublingCube DoublingCube { get; }

    public List<int> RemainingMoves { get; private set; }

    public List<Move> MoveHistory { get; private set; }

    public bool GameStarted { get; private set; }

    public bool GameOver { get; private set; }

    public Player? Winner { get; private set; }

    // Match-related properties
    public bool IsCrawfordGame { get; set; }

    public string? MatchId { get; set; }

    // Opening roll properties
    public bool IsOpeningRoll { get; private set; }

    public int? WhiteOpeningRoll { get; private set; }

    public int? RedOpeningRoll { get; private set; }

    public bool IsOpeningRollTie { get; private set; }

    // Time control properties
    public TimeControlConfig? TimeControl { get; set; }

    public PlayerTimeState? WhiteTimeState { get; private set; }

    public PlayerTimeState? RedTimeState { get; private set; }

    /// <summary>
    /// Start a new game
    /// </summary>
    public void StartNewGame()
    {
        Board.SetupInitialPosition();
        WhitePlayer.CheckersOnBar = 0;
        WhitePlayer.CheckersBornOff = 0;
        RedPlayer.CheckersOnBar = 0;
        RedPlayer.CheckersBornOff = 0;
        DoublingCube.Reset();
        GameStarted = true;
        GameOver = false;
        Winner = null;

        // Start with opening roll phase
        IsOpeningRoll = true;
        WhiteOpeningRoll = null;
        RedOpeningRoll = null;
        IsOpeningRollTie = false;
    }

    /// <summary>
    /// Roll dice for the current player's turn
    /// </summary>
    public void RollDice()
    {
        if (!GameStarted || GameOver)
        {
            throw new InvalidOperationException("Game is not in progress");
        }

        Dice.Roll();
        RemainingMoves = new List<int>(Dice.GetMoves());
        MoveHistory.Clear(); // Clear history for new turn
    }

    /// <summary>
    /// Execute a move
    /// </summary>
    public bool ExecuteMove(Move move)
    {
        if (!IsValidMove(move))
        {
            return false;
        }

        // Capture state BEFORE executing (for undo)
        move.OpponentCheckersOnBarBefore = GetOpponent().CheckersOnBar;
        move.CurrentPlayerBornOffBefore = CurrentPlayer.CheckersBornOff;

        // Handle entering from bar
        if (move.From == 0)
        {
            CurrentPlayer.CheckersOnBar--;

            var destPoint = Board.GetPoint(move.To);
            if (destPoint.IsBlot && destPoint.Color != CurrentPlayer.Color)
            {
                // Hit opponent's blot
                var opponent = GetOpponent();
                destPoint.RemoveChecker();
                opponent.CheckersOnBar++;
                move.IsHit = true;
            }

            destPoint.AddChecker(CurrentPlayer.Color);
        }

        // Handle bearing off
        else if (move.IsBearOff)
        {
            var fromPoint = Board.GetPoint(move.From);
            fromPoint.RemoveChecker();
            CurrentPlayer.CheckersBornOff++;
        }

        // Normal move
        else
        {
            var fromPoint = Board.GetPoint(move.From);
            var toPoint = Board.GetPoint(move.To);

            fromPoint.RemoveChecker();

            if (toPoint.IsBlot && toPoint.Color != CurrentPlayer.Color)
            {
                // Hit opponent's blot
                var opponent = GetOpponent();
                toPoint.RemoveChecker();
                opponent.CheckersOnBar++;
                move.IsHit = true;
            }

            toPoint.AddChecker(CurrentPlayer.Color);
        }

        // Remove the used die from remaining moves
        RemainingMoves.Remove(move.DieValue);

        // Track move in history
        MoveHistory.Add(move);

        // Check for win condition
        if (CurrentPlayer.CheckersBornOff == 15)
        {
            GameOver = true;
            Winner = CurrentPlayer;
        }

        return true;
    }

    /// <summary>
    /// Validate if a move is legal
    /// </summary>
    public bool IsValidMove(Move move)
    {
        // Must have this die value available
        if (!RemainingMoves.Contains(move.DieValue))
        {
            return false;
        }

        // If checkers on bar, must enter first
        if (CurrentPlayer.CheckersOnBar > 0 && move.From != 0)
        {
            return false;
        }

        // Entering from bar
        if (move.From == 0)
        {
            if (CurrentPlayer.CheckersOnBar == 0)
            {
                return false;
            }

            var destPoint = Board.GetPoint(move.To);
            return destPoint.IsOpen(CurrentPlayer.Color);
        }

        // Bearing off
        if (move.IsBearOff)
        {
            return CanBearOff(move.From, move.DieValue);
        }

        // Normal move
        var fromPoint = Board.GetPoint(move.From);
        if (fromPoint.Color != CurrentPlayer.Color || fromPoint.Count == 0)
        {
            return false;
        }

        var toPoint = Board.GetPoint(move.To);
        return toPoint.IsOpen(CurrentPlayer.Color);
    }

    /// <summary>
    /// Get all valid moves for the current state
    /// </summary>
    public List<Move> GetValidMoves()
    {
        var validMoves = new List<Move>();

        // If checkers on bar, must enter
        if (CurrentPlayer.CheckersOnBar > 0)
        {
            foreach (var die in RemainingMoves.Distinct())
            {
                int entryPoint = CurrentPlayer.Color == CheckerColor.White ? 25 - die : die;
                var move = new Move(0, entryPoint, die);
                if (IsValidMove(move))
                {
                    validMoves.Add(move);
                }
            }

            return validMoves;
        }

        // Check bearing off
        if (Board.AreAllCheckersInHomeBoard(CurrentPlayer, CurrentPlayer.CheckersOnBar))
        {
            var (homeStart, homeEnd) = CurrentPlayer.GetHomeBoardRange();
            for (int pos = homeStart; pos <= homeEnd; pos++)
            {
                var point = Board.GetPoint(pos);
                if (point.Color == CurrentPlayer.Color && point.Count > 0)
                {
                    foreach (var die in RemainingMoves.Distinct())
                    {
                        var move = new Move(pos, CurrentPlayer.Color == CheckerColor.White ? 0 : 25, die);
                        if (IsValidMove(move))
                        {
                            validMoves.Add(move);
                        }
                    }
                }
            }
        }

        // Normal moves
        for (int from = 1; from <= 24; from++)
        {
            var fromPoint = Board.GetPoint(from);
            if (fromPoint.Color != CurrentPlayer.Color || fromPoint.Count == 0)
            {
                continue;
            }

            foreach (var die in RemainingMoves.Distinct())
            {
                int to = from + (CurrentPlayer.GetDirection() * die);
                if (to >= 1 && to <= 24)
                {
                    var move = new Move(from, to, die);
                    if (IsValidMove(move))
                    {
                        validMoves.Add(move);
                    }
                }
            }
        }

        return validMoves;
    }

    /// <summary>
    /// End the current player's turn
    /// </summary>
    public void EndTurn()
    {
        RemainingMoves.Clear();
        MoveHistory.Clear();
        Dice.SetDice(0, 0); // Clear dice for next player
        CurrentPlayer = GetOpponent();
    }

    /// <summary>
    /// Undo the last move made during the current turn.
    /// Returns true if undo succeeded, false if no moves to undo.
    /// </summary>
    public bool UndoLastMove()
    {
        if (MoveHistory.Count == 0)
        {
            return false;
        }

        var move = MoveHistory[^1]; // Get last move
        MoveHistory.RemoveAt(MoveHistory.Count - 1);

        // Reverse the move based on type
        if (move.From == 0)
        {
            // Was entering from bar - reverse it
            var destPoint = Board.GetPoint(move.To);
            destPoint.RemoveChecker();
            CurrentPlayer.CheckersOnBar++;

            // If we hit opponent, restore their checker
            if (move.IsHit)
            {
                var opponent = GetOpponent();
                destPoint.AddChecker(opponent.Color);
                opponent.CheckersOnBar = move.OpponentCheckersOnBarBefore;
            }
        }
        else if (move.IsBearOff)
        {
            // Was bearing off - reverse it
            var fromPoint = Board.GetPoint(move.From);
            fromPoint.AddChecker(CurrentPlayer.Color);
            CurrentPlayer.CheckersBornOff = move.CurrentPlayerBornOffBefore;
        }
        else
        {
            // Normal move - reverse it
            var fromPoint = Board.GetPoint(move.From);
            var toPoint = Board.GetPoint(move.To);

            toPoint.RemoveChecker();
            fromPoint.AddChecker(CurrentPlayer.Color);

            // If we hit opponent, restore their checker
            if (move.IsHit)
            {
                var opponent = GetOpponent();
                toPoint.AddChecker(opponent.Color);
                opponent.CheckersOnBar = move.OpponentCheckersOnBarBefore;
            }
        }

        // Restore die value to remaining moves
        RemainingMoves.Add(move.DieValue);
        RemainingMoves.Sort();
        RemainingMoves.Reverse(); // Keep largest first

        return true;
    }

    /// <summary>
    /// Get the opponent of the current player
    /// </summary>
    public Player GetOpponent()
    {
        return CurrentPlayer.Color == CheckerColor.White ? RedPlayer : WhitePlayer;
    }

    /// <summary>
    /// Offer a double to the opponent
    /// </summary>
    public bool OfferDouble()
    {
        // Crawford rule - no doubling allowed
        if (IsCrawfordGame)
        {
            return false;
        }

        return DoublingCube.CanDouble(CurrentPlayer.Color);
    }

    /// <summary>
    /// Accept a double
    /// </summary>
    public void AcceptDouble()
    {
        DoublingCube.Double(GetOpponent().Color);
    }

    /// <summary>
    /// Forfeit the game, setting the specified player as the winner
    /// </summary>
    public void ForfeitGame(Player winner)
    {
        if (!GameStarted || GameOver)
        {
            throw new InvalidOperationException("Cannot forfeit - game is not in progress");
        }

        Winner = winner;
        GameOver = true;
    }

    /// <summary>
    /// Calculate game result (normal, gammon, or backgammon)
    /// </summary>
    public int GetGameResult()
    {
        if (!GameOver || Winner == null)
        {
            return 0;
        }

        var loser = Winner.Color == CheckerColor.White ? RedPlayer : WhitePlayer;

        // Backgammon (3x) - loser has checkers on bar or in winner's home board
        if (loser.CheckersBornOff == 0)
        {
            if (loser.CheckersOnBar > 0)
            {
                return 3 * DoublingCube.Value;
            }

            var (winnerHomeStart, winnerHomeEnd) = Winner.GetHomeBoardRange();
            for (int i = winnerHomeStart; i <= winnerHomeEnd; i++)
            {
                var point = Board.GetPoint(i);
                if (point.Color == loser.Color && point.Count > 0)
                {
                    return 3 * DoublingCube.Value;
                }
            }

            // Gammon (2x) - loser has not borne off any checkers
            return 2 * DoublingCube.Value;
        }

        // Normal win (1x)
        return DoublingCube.Value;
    }

    /// <summary>
    /// Determine the type of win (Normal, Gammon, or Backgammon)
    /// </summary>
    public WinType DetermineWinType()
    {
        if (!GameOver || Winner == null)
        {
            throw new InvalidOperationException("Game is not over yet");
        }

        var loser = Winner.Color == CheckerColor.White ? RedPlayer : WhitePlayer;

        // If loser hasn't borne off any checkers
        if (loser.CheckersBornOff == 0)
        {
            // Backgammon - loser has checkers on bar or in winner's home board
            if (loser.CheckersOnBar > 0)
            {
                return WinType.Backgammon;
            }

            var (winnerHomeStart, winnerHomeEnd) = Winner.GetHomeBoardRange();
            for (int i = winnerHomeStart; i <= winnerHomeEnd; i++)
            {
                var point = Board.GetPoint(i);
                if (point.Color == loser.Color && point.Count > 0)
                {
                    return WinType.Backgammon;
                }
            }

            // Gammon - loser has not borne off any checkers
            return WinType.Gammon;
        }

        // Normal win
        return WinType.Normal;
    }

    /// <summary>
    /// Create a GameResult for the current game state
    /// </summary>
    public GameResult CreateGameResult()
    {
        if (!GameOver || Winner == null)
        {
            throw new InvalidOperationException("Game is not over yet");
        }

        var winType = DetermineWinType();
        return new GameResult(Winner.Color == CheckerColor.White ? WhitePlayer.Name : RedPlayer.Name, winType, DoublingCube.Value);
    }

    /// <summary>
    /// Set the current player (for position import/testing)
    /// </summary>
    public void SetCurrentPlayer(CheckerColor color)
    {
        CurrentPlayer = color == CheckerColor.White ? WhitePlayer : RedPlayer;
    }

    /// <summary>
    /// Set game started flag (for position import/testing)
    /// </summary>
    public void SetGameStarted(bool started)
    {
        GameStarted = started;
    }

    /// <summary>
    /// Roll a single die for the opening roll (each player clicks to roll)
    /// </summary>
    /// <param name="color">Which player is rolling</param>
    /// <returns>The die value rolled, or -1 if both players must re-roll (tie)</returns>
    public int RollOpening(CheckerColor color)
    {
        if (!IsOpeningRoll)
        {
            throw new InvalidOperationException("Not in opening roll phase");
        }

        // If there was a tie, clear the previous rolls when a new roll is made
        if (IsOpeningRollTie)
        {
            WhiteOpeningRoll = null;
            RedOpeningRoll = null;
            IsOpeningRollTie = false;
        }

        int roll = Dice.RollSingle();

        if (color == CheckerColor.White)
        {
            WhiteOpeningRoll = roll;
        }
        else
        {
            RedOpeningRoll = roll;
        }

        // Check if both players have rolled
        if (WhiteOpeningRoll.HasValue && RedOpeningRoll.HasValue)
        {
            // Check for tie
            if (WhiteOpeningRoll.Value == RedOpeningRoll.Value)
            {
                // Tie - keep the dice visible and set tie flag
                IsOpeningRollTie = true;
                return -1; // Signal a tie
            }

            // Different numbers rolled - clear tie flag and determine winner
            IsOpeningRollTie = false;

            // Determine winner and set up first turn
            // Always set dice in same order (White, Red) - don't sort by value
            Dice.SetDice(WhiteOpeningRoll.Value, RedOpeningRoll.Value);

            if (WhiteOpeningRoll.Value > RedOpeningRoll.Value)
            {
                CurrentPlayer = WhitePlayer;
            }
            else
            {
                CurrentPlayer = RedPlayer;
            }

            RemainingMoves = new List<int>(Dice.GetMoves());
            IsOpeningRoll = false;
        }

        return roll;
    }

    /// <summary>
    /// Initialize time controls for this game
    /// </summary>
    public void InitializeTimeControl(TimeControlConfig config, TimeSpan whiteReserve, TimeSpan redReserve)
    {
        TimeControl = config;
        WhiteTimeState = new PlayerTimeState { ReserveTime = whiteReserve };
        RedTimeState = new PlayerTimeState { ReserveTime = redReserve };
    }

    /// <summary>
    /// Check if current player has timed out
    /// </summary>
    public bool HasCurrentPlayerTimedOut()
    {
        if (TimeControl == null || TimeControl.Type == TimeControlType.None)
        {
            return false;
        }

        var timeState = CurrentPlayer.Color == CheckerColor.White ? WhiteTimeState : RedTimeState;
        return timeState?.HasTimedOut(TimeControl.DelaySeconds) ?? false;
    }

    /// <summary>
    /// Start turn timer for current player
    /// </summary>
    public void StartTurnTimer()
    {
        if (TimeControl == null || TimeControl.Type == TimeControlType.None)
        {
            return;
        }

        Console.WriteLine($"[TIME DEBUG] StartTurnTimer called");
        Console.WriteLine($"[TIME DEBUG]   CurrentPlayer: {CurrentPlayer?.Color}");
        Console.WriteLine($"[TIME DEBUG]   IsOpeningRoll: {IsOpeningRoll}");

        if (CurrentPlayer == null)
        {
            Console.WriteLine($"[TIME DEBUG]   ERROR: CurrentPlayer is null, cannot start timer!");
            return;
        }

        var timeState = CurrentPlayer.Color == CheckerColor.White ? WhiteTimeState : RedTimeState;
        timeState?.StartTurn();

        Console.WriteLine($"[TIME DEBUG]   TurnStartTime set to: {timeState?.TurnStartTime}");
    }

    /// <summary>
    /// End turn timer for current player
    /// </summary>
    public void EndTurnTimer()
    {
        if (TimeControl == null || TimeControl.Type == TimeControlType.None)
        {
            return;
        }

        var timeState = CurrentPlayer.Color == CheckerColor.White ? WhiteTimeState : RedTimeState;
        timeState?.EndTurn(TimeControl.DelaySeconds);
    }

    /// <summary>
    /// Determine which player goes first by rolling dice
    /// </summary>
    private void DetermineFirstPlayer()
    {
        int whiteRoll, redRoll;
        do
        {
            whiteRoll = Dice.RollSingle();
            redRoll = Dice.RollSingle();
        }
        while (whiteRoll == redRoll);

        CurrentPlayer = whiteRoll > redRoll ? WhitePlayer : RedPlayer;

        // First player must manually roll dice to start their turn
        // (Opening rolls only used to determine turn order)
    }

    /// <summary>
    /// Check if bearing off from a point is valid
    /// </summary>
    private bool CanBearOff(int from, int dieValue)
    {
        // Must have all checkers in home board
        if (!Board.AreAllCheckersInHomeBoard(CurrentPlayer, CurrentPlayer.CheckersOnBar))
        {
            return false;
        }

        var fromPoint = Board.GetPoint(from);
        if (fromPoint.Color != CurrentPlayer.Color || fromPoint.Count == 0)
        {
            return false;
        }

        var (homeStart, homeEnd) = CurrentPlayer.GetHomeBoardRange();

        // Point must be in home board
        if (CurrentPlayer.Color == CheckerColor.White)
        {
            if (from < homeStart || from > homeEnd)
            {
                return false;
            }

            // Exact die value match
            if (from == dieValue)
            {
                return true;
            }

            // If die is higher than point, can bear off if no checkers on higher points
            if (dieValue > from)
            {
                int highestPoint = Board.GetHighestPoint(CurrentPlayer.Color);
                return from == highestPoint;
            }
        }

        // Red
        else
        {
            if (from < homeStart || from > homeEnd)
            {
                return false;
            }

            int normalizedPosition = 25 - from; // Convert to 1-6 range

            // Exact die value match
            if (normalizedPosition == dieValue)
            {
                return true;
            }

            // If die is higher than normalized position, can bear off if no checkers on higher points
            if (dieValue > normalizedPosition)
            {
                int highestPoint = Board.GetHighestPoint(CurrentPlayer.Color);
                return from == highestPoint;
            }
        }

        return false;
    }
}
