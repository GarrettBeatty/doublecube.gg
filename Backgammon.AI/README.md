# Backgammon AI Simulator

This project allows you to create AI players that can play Backgammon against each other using the game engine directly.

## Architecture

### IBackgammonAI Interface

All AI players must implement the `IBackgammonAI` interface:

```csharp
public interface IBackgammonAI
{
    string Name { get; }
    List<Move> ChooseMoves(GameEngine engine);
    bool ShouldAcceptDouble(GameEngine engine);
    bool ShouldOfferDouble(GameEngine engine);
}
```

The AI has full access to the `GameEngine` to inspect:
- Board state (`engine.Board`)
- Current player (`engine.CurrentPlayer`)
- Available moves (`engine.GetValidMoves()`)
- Remaining dice (`engine.RemainingMoves`)
- Opponent information (`engine.GetOpponent()`)

### Included AI Implementations

**RandomAI** - Makes random valid moves. Useful as a baseline for testing more sophisticated AIs.

## Usage

### Running Simulations

```bash
cd Backgammon.AI
dotnet run
```

The program will:
1. Ask how many games to simulate
2. Run the simulation between two AIs
3. Display win statistics
4. Show one example game with verbose output

### Creating Your Own AI

Create a new class implementing `IBackgammonAI`:

```csharp
public class MySmartAI : IBackgammonAI
{
    public string Name => "MySmartAI";

    public List<Move> ChooseMoves(GameEngine engine)
    {
        var chosenMoves = new List<Move>();
        
        while (engine.RemainingMoves.Count > 0)
        {
            var validMoves = engine.GetValidMoves();
            if (validMoves.Count == 0) break;

            // Your strategy here
            var bestMove = PickBestMove(validMoves, engine);
            
            if (engine.ExecuteMove(bestMove))
                chosenMoves.Add(bestMove);
            else
                break;
        }
        
        return chosenMoves;
    }

    public bool ShouldAcceptDouble(GameEngine engine)
    {
        // Your doubling strategy
        return true;
    }

    public bool ShouldOfferDouble(GameEngine engine)
    {
        return false;
    }

    private Move PickBestMove(List<Move> moves, GameEngine engine)
    {
        // Implement your move selection logic
        return moves[0];
    }
}
```

### Using AISimulator Programmatically

```csharp
// Create AIs
var ai1 = new RandomAI("AI-1");
var ai2 = new MySmartAI();

// Create simulator
var simulator = new AISimulator(ai1, ai2, verbose: false);

// Run a single game
var result = simulator.RunGame();
Console.WriteLine($"Winner: {result.Winner}, Points: {result.Points}");

// Run multiple games
var stats = simulator.RunSimulation(1000);
stats.PrintSummary();
```

## Strategy Ideas

Some ideas for creating more sophisticated AIs:

1. **Defensive AI** - Avoid leaving blots, prioritize making points
2. **Aggressive AI** - Hit opponent's blots when possible
3. **Racing AI** - Focus on pip count, optimize bearing off
4. **Positional AI** - Evaluate board positions with heuristics
5. **Monte Carlo AI** - Simulate future positions to evaluate moves
6. **Neural Network AI** - Train on game data

## Game Engine Access

The AI has access to these key methods:

- `engine.GetValidMoves()` - Get all legal moves for current state
- `engine.ExecuteMove(move)` - Execute a move
- `engine.Board.GetPoint(position)` - Inspect any point on the board
- `engine.CurrentPlayer` - Current player info
- `engine.GetOpponent()` - Opponent player info
- `engine.RemainingMoves` - Available dice to use

The AI doesn't need to:
- Handle dice rolling (simulator does this)
- Manage turn switching (simulator does this)
- Validate moves manually (use `GetValidMoves()`)
