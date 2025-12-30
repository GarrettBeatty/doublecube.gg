namespace Backgammon.Core;

/// <summary>
/// Main game engine that manages the backgammon game state and rules
/// </summary>
public class GameEngine
{
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

        // Determine who goes first
        DetermineFirstPlayer();
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
        } while (whiteRoll == redRoll);

        CurrentPlayer = whiteRoll > redRoll ? WhitePlayer : RedPlayer;
        
        // Use the opening rolls as the first move
        Dice.SetDice(Math.Max(whiteRoll, redRoll), Math.Min(whiteRoll, redRoll));
        RemainingMoves = Dice.GetMoves();
    }

    /// <summary>
    /// Roll dice for the current player's turn
    /// </summary>
    public void RollDice()
    {
        if (!GameStarted || GameOver)
            throw new InvalidOperationException("Game is not in progress");

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
            return false;

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
            return false;

        // If checkers on bar, must enter first
        if (CurrentPlayer.CheckersOnBar > 0 && move.From != 0)
            return false;

        // Entering from bar
        if (move.From == 0)
        {
            if (CurrentPlayer.CheckersOnBar == 0)
                return false;

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
            return false;

        var toPoint = Board.GetPoint(move.To);
        return toPoint.IsOpen(CurrentPlayer.Color);
    }

    /// <summary>
    /// Check if bearing off from a point is valid
    /// </summary>
    private bool CanBearOff(int from, int dieValue)
    {
        // Must have all checkers in home board
        if (!Board.AreAllCheckersInHomeBoard(CurrentPlayer, CurrentPlayer.CheckersOnBar))
            return false;

        var fromPoint = Board.GetPoint(from);
        if (fromPoint.Color != CurrentPlayer.Color || fromPoint.Count == 0)
            return false;

        var (homeStart, homeEnd) = CurrentPlayer.GetHomeBoardRange();
        
        // Point must be in home board
        if (CurrentPlayer.Color == CheckerColor.White)
        {
            if (from < homeStart || from > homeEnd)
                return false;

            // Exact die value match
            if (from == dieValue)
                return true;

            // If die is higher than point, can bear off if no checkers on higher points
            if (dieValue > from)
            {
                int highestPoint = Board.GetHighestPoint(CurrentPlayer.Color);
                return from == highestPoint;
            }
        }
        else // Red
        {
            if (from < homeStart || from > homeEnd)
                return false;

            int normalizedPosition = 25 - from; // Convert to 1-6 range
            
            // Exact die value match
            if (normalizedPosition == dieValue)
                return true;

            // If die is higher than normalized position, can bear off if no checkers on higher points
            if (dieValue > normalizedPosition)
            {
                int highestPoint = Board.GetHighestPoint(CurrentPlayer.Color);
                return from == highestPoint;
            }
        }

        return false;
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
                    validMoves.Add(move);
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
                            validMoves.Add(move);
                    }
                }
            }
        }

        // Normal moves
        for (int from = 1; from <= 24; from++)
        {
            var fromPoint = Board.GetPoint(from);
            if (fromPoint.Color != CurrentPlayer.Color || fromPoint.Count == 0)
                continue;

            foreach (var die in RemainingMoves.Distinct())
            {
                int to = from + (CurrentPlayer.GetDirection() * die);
                if (to >= 1 && to <= 24)
                {
                    var move = new Move(from, to, die);
                    if (IsValidMove(move))
                        validMoves.Add(move);
                }
            }
        }

        return validMoves;
    }

    /// <summary>
    /// Undo the last move made during this turn
    /// </summary>
    public bool UndoLastMove()
    {
        if (MoveHistory.Count == 0)
            return false;

        var lastMove = MoveHistory[MoveHistory.Count - 1];
        MoveHistory.RemoveAt(MoveHistory.Count - 1);

        // Determine the die value used
        int dieValue = Math.Abs(lastMove.To - lastMove.From);
        
        // Special handling for bearing off with higher die
        if (lastMove.To == 0 || lastMove.To == 25)
        {
            // For bear-off, use the exact die value
            var homeBoard = CurrentPlayer.Color == CheckerColor.White ? 
                Enumerable.Range(1, 6) : Enumerable.Range(19, 6);
            var furthestPoint = homeBoard.Where(p => Board.GetPoint(p).Color == CurrentPlayer.Color)
                                         .OrderBy(p => CurrentPlayer.Color == CheckerColor.White ? p : -p)
                                         .FirstOrDefault();
            if (furthestPoint > 0)
            {
                int exactDist = Math.Abs(furthestPoint - (CurrentPlayer.Color == CheckerColor.White ? 0 : 25));
                // Find the die that was used (could be exact or higher)
                if (Dice.Die1 >= exactDist && !RemainingMoves.Contains(Dice.Die1))
                    dieValue = Dice.Die1;
                else if (Dice.Die2 >= exactDist && !RemainingMoves.Contains(Dice.Die2))
                    dieValue = Dice.Die2;
            }
        }

        // Reverse the move on the board
        if (lastMove.From == 0)
        {
            // Was entering from bar - put back on bar
            CurrentPlayer.CheckersOnBar++;
            var destPoint = Board.GetPoint(lastMove.To);
            destPoint.RemoveChecker();
        }
        else if (lastMove.To == 0 || lastMove.To == 25)
        {
            // Was bearing off - put back on board
            CurrentPlayer.CheckersBornOff--;
            Board.GetPoint(lastMove.From).AddChecker(CurrentPlayer.Color);
        }
        else
        {
            // Regular move - reverse it
            var fromPoint = Board.GetPoint(lastMove.From);
            var toPoint = Board.GetPoint(lastMove.To);
            
            // Check if we hit an opponent (they'd be on bar now)
            var opponent = GetOpponent();
            if (opponent.CheckersOnBar > 0)
            {
                // Put opponent's checker back
                opponent.CheckersOnBar--;
                toPoint.AddChecker(opponent.Color);
            }
            
            // Move our checker back
            toPoint.RemoveChecker();
            fromPoint.AddChecker(CurrentPlayer.Color);
        }

        // Restore the die to remaining moves
        RemainingMoves.Add(dieValue);
        RemainingMoves.Sort();
        RemainingMoves.Reverse(); // Keep larger values first

        return true;
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
            throw new InvalidOperationException("Cannot forfeit - game is not in progress");

        Winner = winner;
        GameOver = true;
    }

    /// <summary>
    /// Calculate game result (normal, gammon, or backgammon)
    /// </summary>
    public int GetGameResult()
    {
        if (!GameOver || Winner == null)
            return 0;

        var loser = Winner.Color == CheckerColor.White ? RedPlayer : WhitePlayer;

        // Backgammon (3x) - loser has checkers on bar or in winner's home board
        if (loser.CheckersBornOff == 0)
        {
            if (loser.CheckersOnBar > 0)
                return 3 * DoublingCube.Value;

            var (winnerHomeStart, winnerHomeEnd) = Winner.GetHomeBoardRange();
            for (int i = winnerHomeStart; i <= winnerHomeEnd; i++)
            {
                var point = Board.GetPoint(i);
                if (point.Color == loser.Color && point.Count > 0)
                    return 3 * DoublingCube.Value;
            }

            // Gammon (2x) - loser has not borne off any checkers
            return 2 * DoublingCube.Value;
        }

        // Normal win (1x)
        return DoublingCube.Value;
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
}
