---
sidebar_position: 2
---

# Core Engine

The `Backgammon.Core` project contains all game logic with **zero external dependencies**.

## Key Classes

### GameEngine

The main game orchestrator that enforces all backgammon rules.

```csharp
var game = new GameEngine("White", "Red");
game.StartNewGame();

// Roll dice
game.RollDice();

// Get valid moves
var moves = game.GetValidMoves();

// Execute a move
game.ExecuteMove(moves[0]);

// End turn
game.EndTurn();
```

**Key Properties:**
- `CurrentPlayer` - Player whose turn it is
- `RemainingMoves` - Dice values left to use
- `GameOver` - Whether the game has ended
- `Winner` - The winning player (if game over)

### Board

Represents the 24-point backgammon board.

```csharp
// Points are 1-24
// Point 0 = bar
// Point 25 = bear off zone

var point = board.GetPoint(6);
int checkers = point.Checkers.Count;
CheckerColor color = point.Color;
```

**Movement Directions:**
- **White**: Moves from point 24 → point 1 (descending)
- **Red**: Moves from point 1 → point 24 (ascending)

**Home Boards:**
- White home: Points 1-6
- Red home: Points 19-24

### Match

Tracks multi-game matches with scoring.

```csharp
var match = new Match(7); // First to 7 points

match.RecordGameResult(new GameResult
{
    Winner = CheckerColor.White,
    WinType = WinType.Gammon,
    PointsScored = 4 // 2 for gammon × 2 for cube
});

bool isComplete = match.IsComplete;
bool isCrawford = match.IsCrawfordGame;
```

**Crawford Rule:**
When a player reaches match point minus one, the next game is the "Crawford game" where no doubling is allowed.

### DoublingCube

Manages stake doubling.

```csharp
var cube = new DoublingCube();

// Initial value is 1, centered
cube.Offer(CheckerColor.White);
cube.Accept(CheckerColor.Red); // Now at 2, Red owns

// Only the non-owner can offer
cube.Offer(CheckerColor.Red);
cube.Accept(CheckerColor.White); // Now at 4, White owns
```

### Move

Represents a checker movement.

```csharp
var move = new Move(from: 6, to: 3);

// Special cases
var barEntry = new Move(from: 0, to: 22); // Entering from bar
var bearOff = new Move(from: 3, to: 25);  // Bearing off
```

## Game Rules

### Valid Move Requirements

1. **Bar Priority**: If checkers are on the bar, they must enter first
2. **Open Point**: Can only land on points with ≤1 opponent checker
3. **Hitting**: Landing on a single opponent checker sends it to the bar
4. **Bear Off**: Only when all checkers are in home board
5. **Forced Move**: If only one die can be used, must use the larger value

### Winning Conditions

| Type | Condition | Points |
|------|-----------|--------|
| Normal | Opponent has borne off ≥1 checker | 1 × cube |
| Gammon | Opponent has 0 checkers borne off | 2 × cube |
| Backgammon | Gammon + checker on bar or in winner's home | 3 × cube |

## Testing the Engine

```csharp
[Fact]
public void TestBasicMove()
{
    var game = new GameEngine();
    game.StartNewGame();

    // Manipulate board for testing
    game.Board.GetPoint(1).Checkers.Clear();
    game.Board.GetPoint(1).AddChecker(CheckerColor.White);

    // Set specific dice
    game.Dice.SetDice(3, 4);
    game.RemainingMoves.Clear();
    game.RemainingMoves.AddRange(game.Dice.GetMoves());

    var moves = game.GetValidMoves();
    Assert.NotEmpty(moves);
}
```
