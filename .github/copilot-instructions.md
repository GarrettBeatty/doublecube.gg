# Backgammon .NET - AI Coding Agent Instructions

## Project Architecture

This is a **Backgammon game engine** implemented in C# .NET 10.0, structured as a multi-project solution:

- **Backgammon.Core**: Game logic library (board, rules, engine)
- **Backgammon.Console**: Interactive console UI using Spectre.Console
- **Backgammon.AI**: AI simulation framework with pluggable AI implementations
- **Backgammon.Tests**: xUnit test suite

## Core Domain Model

### Board Representation
- **24 points** (positions 1-24) implemented as `Point[]` array
- **Point 0 = bar** for entering checkers
- **Point 25 = bear off** destination
- **White moves 24→1** (descending), **Red moves 1→24** (ascending)
- Initial setup in `Board.SetupInitialPosition()`: White at points 24,13,8,6; Red at 1,12,17,19

### Player Movement Rules
```csharp
// CRITICAL: Direction is player-dependent
WhitePlayer.GetDirection() // returns -1 (moves toward 1)
RedPlayer.GetDirection()   // returns +1 (moves toward 24)

// Home boards are OPPOSITE
White: points 1-6
Red: points 19-24
```

### The GameEngine Lifecycle Pattern
**Always follow this sequence when implementing AI or game logic:**

```csharp
1. Check game.RemainingMoves.Count > 0
2. If 0, call game.RollDice() to populate RemainingMoves
3. Call game.GetValidMoves() to get legal moves
4. For each move: game.ExecuteMove(move) automatically:
   - Removes die from RemainingMoves
   - Updates board state
   - Handles hits (sends to bar)
   - Checks win conditions
5. Call game.EndTurn() to switch players
```

**Never manually manipulate `RemainingMoves`** - `ExecuteMove()` manages it.

## Key Design Patterns

### Move Validation & Execution
The engine enforces complex Backgammon rules automatically:

- **Bar priority**: When `CurrentPlayer.CheckersOnBar > 0`, only bar entry moves are valid
- **Bearing off**: Only valid when `Board.AreAllCheckersInHomeBoard()` returns true
- **Exact die usage**: Must use exact die values; bearing off allows exact-or-higher from furthest point
- **Forced moves**: If only one die can be used, must use the larger value

### AI Implementation Contract
All AIs implement `IBackgammonAI` with this pattern:

```csharp
public List<Move> ChooseMoves(GameEngine engine)
{
    var chosen = new List<Move>();
    
    while (engine.RemainingMoves.Count > 0)
    {
        var validMoves = engine.GetValidMoves();
        if (validMoves.Count == 0) break;
        
        var move = /* your strategy */;
        if (engine.ExecuteMove(move))
            chosen.Add(move);
        else
            break; // Safety: invalid move
    }
    return chosen;
}
```

**Key insight**: `ExecuteMove()` modifies engine state, so `GetValidMoves()` changes each iteration.

### AISimulator Usage Pattern
Located in `Backgammon.AI/AISimulator.cs`, run simulations with:

```csharp
var sim = new AISimulator(whiteAI, redAI, verbose: true);
var result = sim.RunGame(); // Returns GameResult with winner + stakes
sim.RunMultipleGames(count); // Returns win statistics
```

## Build & Test Commands

```bash
# Build entire solution
dotnet build

# Run console game
cd Backgammon.Console && dotnet run

# Run AI simulations
cd Backgammon.AI && dotnet run

# Run tests
dotnet test
dotnet test --verbosity normal  # Detailed output
```

## Testing Conventions

Tests use **xUnit** with this pattern from `UnitTest1.cs`:

```csharp
// Setup clean state
var game = new GameEngine();
game.StartNewGame();

// Manipulate board directly for scenarios
game.Board.GetPoint(1).Checkers.Clear();
game.Board.GetPoint(1).AddChecker(CheckerColor.White);

// Set specific dice rolls
game.Dice.SetDice(3, 4);
game.RemainingMoves.Clear();
game.RemainingMoves.AddRange(game.Dice.GetMoves());
```

**When writing tests**: Directly manipulate `Board.GetPoint(n)` and `Dice.SetDice()` rather than simulating full games.

## Common Pitfalls

1. **Point numbering confusion**: Remember bar=0, board=1-24, bear-off=0 or 25 (depends on color)
2. **Move direction**: Always use `Player.GetDirection()` instead of hardcoding -1/+1
3. **Doubles handling**: `Dice.GetMoves()` returns 4 values for doubles (e.g., [6,6,6,6])
4. **Bearing off rules**: Requires ALL checkers in home board; can use higher die from furthest point
5. **AI state mutation**: `GetValidMoves()` returns new list each call after `ExecuteMove()` changes state

## File Organization

- Core game logic has **no UI dependencies** - pure domain models
- Console project uses **Spectre.Console** for rendering (see `DrawBoard()` in Program.cs)
- AI implementations are **stateless** - all state in GameEngine passed as parameter
- Tests directly manipulate internal state for scenario setup (public properties by design)

## When Adding New AI

1. Implement `IBackgammonAI` in `Backgammon.AI/`
2. Follow the `ChooseMoves()` pattern shown above
3. Test against `RandomAI` baseline using `AISimulator`
4. Consider priorities: bear-off > hit > advance (see `GreedyAI.cs`)

## Debugging Tips

- `GameEngine.Verbose` mode doesn't exist - use `AISimulator(verbose: true)` for detailed logs
- Check `CurrentPlayer.CheckersOnBar` before assuming normal moves are valid
- Use `engine.GetGameResult()` to get full win type (normal/gammon/backgammon) with stakes
