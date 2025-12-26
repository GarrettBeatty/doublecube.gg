# Example: Using the AI Framework Programmatically

This example shows how to use the Backgammon.AI framework in your own C# applications.

## Basic Usage

```csharp
using Backgammon.AI;

// Create two AI players
var ai1 = new RandomAI("Bobby");
var ai2 = new GreedyAI("Carol");

// Create simulator
var simulator = new AISimulator(ai1, ai2, verbose: false);

// Run a single game
var result = simulator.RunGame();
Console.WriteLine($"Winner: {result.Winner}");
Console.WriteLine($"Points: {result.Points}");
Console.WriteLine($"Turns: {result.Turns}");

// Run multiple games for statistics
var stats = simulator.RunSimulation(1000);
stats.PrintSummary();
```

## Creating a Custom AI

```csharp
using Backgammon.Core;
using Backgammon.AI;

public class DefensiveAI : IBackgammonAI
{
    public string Name => "Defensive AI";

    public List<Move> ChooseMoves(GameEngine engine)
    {
        var chosenMoves = new List<Move>();
        
        while (engine.RemainingMoves.Count > 0)
        {
            var validMoves = engine.GetValidMoves();
            if (validMoves.Count == 0) break;

            // Strategy: Avoid leaving blots when possible
            var safeMove = validMoves.FirstOrDefault(m => IsSafeMove(m, engine));
            var moveToMake = safeMove ?? validMoves.First();
            
            if (engine.ExecuteMove(moveToMake))
                chosenMoves.Add(moveToMake);
            else
                break;
        }
        
        return chosenMoves;
    }

    private bool IsSafeMove(Move move, GameEngine engine)
    {
        // Check if destination has at least 2 of our checkers
        // or will have 2+ after this move
        var point = engine.Board.GetPoint(move.To);
        return point.Color == engine.CurrentPlayer.Color && point.Count >= 1;
    }

    public bool ShouldAcceptDouble(GameEngine engine)
    {
        // Conservative: only accept if close in race
        return engine.CurrentPlayer.CheckersBornOff >= 3;
    }

    public bool ShouldOfferDouble(GameEngine engine)
    {
        return false; // Very conservative
    }
}
```

## Advanced: Analyzing Board State

```csharp
public class AnalyticalAI : IBackgammonAI
{
    public string Name => "Analytical AI";

    public List<Move> ChooseMoves(GameEngine engine)
    {
        var chosenMoves = new List<Move>();
        
        while (engine.RemainingMoves.Count > 0)
        {
            var validMoves = engine.GetValidMoves();
            if (validMoves.Count == 0) break;

            // Evaluate each move
            var bestMove = validMoves
                .Select(m => new { Move = m, Score = EvaluateMove(m, engine) })
                .OrderByDescending(x => x.Score)
                .First()
                .Move;
            
            if (engine.ExecuteMove(bestMove))
                chosenMoves.Add(bestMove);
            else
                break;
        }
        
        return chosenMoves;
    }

    private double EvaluateMove(Move move, GameEngine engine)
    {
        double score = 0;
        
        // Prioritize bearing off
        if (move.IsBearOff)
            score += 100;
        
        // Hitting is good
        var toPoint = engine.Board.GetPoint(move.To);
        if (toPoint.IsBlot && toPoint.Color != engine.CurrentPlayer.Color)
            score += 50;
        
        // Moving towards home is good
        if (engine.CurrentPlayer.Color == CheckerColor.White)
            score += (move.From - move.To) * 2; // Larger movement = better
        else
            score += (move.To - move.From) * 2;
        
        // Avoid leaving blots
        var fromPoint = engine.Board.GetPoint(move.From);
        if (fromPoint.Count == 2) // Will leave a blot
            score -= 30;
        
        return score;
    }

    public bool ShouldAcceptDouble(GameEngine engine)
    {
        return CalculateWinProbability(engine) > 0.25;
    }

    public bool ShouldOfferDouble(GameEngine engine)
    {
        return CalculateWinProbability(engine) > 0.7;
    }

    private double CalculateWinProbability(GameEngine engine)
    {
        // Simple heuristic based on checkers borne off
        var myBornOff = engine.CurrentPlayer.CheckersBornOff;
        var oppBornOff = engine.GetOpponent().CheckersBornOff;
        
        if (myBornOff + oppBornOff == 0)
            return 0.5; // Equal at start
        
        return (double)myBornOff / (myBornOff + oppBornOff);
    }
}
```

## Running Tournaments

```csharp
// Compare multiple AIs
var ais = new List<IBackgammonAI>
{
    new RandomAI("Random"),
    new GreedyAI("Greedy"),
    new DefensiveAI(),
    new AnalyticalAI()
};

// Round-robin tournament
foreach (var ai1 in ais)
{
    foreach (var ai2 in ais)
    {
        if (ai1 == ai2) continue;
        
        var simulator = new AISimulator(ai1, ai2, verbose: false);
        var stats = simulator.RunSimulation(100);
        
        Console.WriteLine($"{ai1.Name} vs {ai2.Name}:");
        Console.WriteLine($"  {ai1.Name}: {stats.WhiteWins} wins ({stats.WhiteWinPercentage:F1}%)");
        Console.WriteLine($"  {ai2.Name}: {stats.RedWins} wins ({stats.RedWinPercentage:F1}%)");
        Console.WriteLine();
    }
}
```

## Performance Tips

1. **Disable verbose mode** when running many games
2. **Use parallel execution** for independent simulations:
   ```csharp
   var results = Parallel.For(0, 1000, i => simulator.RunGame());
   ```
3. **Profile your evaluation functions** - they run thousands of times per game
4. **Cache board evaluations** if computing expensive heuristics
