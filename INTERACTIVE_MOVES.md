# Interactive Move System

## Overview

The web client now supports interactive checker movement with visual highlighting and turn control:

1. **Valid checkers are highlighted** in yellow when it's your turn
2. **Click a checker** to select it (turns green)
3. **Valid destinations are highlighted** in blue
4. **Click a destination** to execute the move
5. **Hit moves** are highlighted in red
6. **Undo button** - undo your last move (before ending turn)
7. **End Turn button** - explicitly end your turn and pass to opponent

## How It Works

### Client-Side (game.js)

#### State Management
```javascript
let currentGameState = null;  // Current game state
let selectedChecker = null;   // { point: number }
let validDestinations = [];   // Array of MoveDto objects
```

#### Click Handling Flow

1. **User clicks canvas** → `handleBoardClick(event)`
2. **Convert click to board coordinates** → `getPointAtPosition(x, y)`
3. **Two scenarios**:
   - **No selection**: Select checker → `selectChecker(point)`
   - **Has selection**: Execute move → `executeMove(from, to)`

#### Visual Feedback

- **Yellow overlay**: Points with checkers that can be moved
- **Green overlay**: Currently selected checker
- **Blue overlay**: Valid destinations for selected checker
- **Red overlay**: Destinations that will hit opponent

### Server-Side (GameHub.cs)

#### New API Methods

```csharp
// Get all points that have movable checkers
Task<List<int>> GetValidSources()

// Get valid destinations from a specific point
Task<List<MoveDto>> GetValidDestinations(int fromPoint)
```

These methods:
- Check if it's the player's turn
- Verify remaining moves exist
- Filter `Engine.GetValidMoves()` by source point
- Return structured move data with hit information

### Rendering (renderBoard function)

The highlighting is drawn in layers:

1. **Background**: Draw board and triangles
2. **Highlights**: Draw colored overlays
   - Valid sources (yellow)
   - Selected checker (green)
   - Valid destinations (blue/red)
3. **Checkers**: Draw actual game pieces on top

## Usage Example

```javascript
// 1. User sees yellow highlights on their checkers
//    (automatically shown when state.isYourTurn && state.remainingMoves.length > 0)

// 2. User clicks point 6 (has white checkers)
handleBoardClick(event)
  → getPointAtPosition(x, y) returns 6
  → selectChecker(6)
    → connection.invoke("GetValidDestinations", 6)
    → Server returns [{ from: 6, to: 3, dieValue: 3 }, { from: 6, to: 1, dieValue: 5 }]
    → Points 3 and 1 now highlighted in blue

// 3. User clicks point 3
handleBoardClick(event)
  → getPointAtPosition(x, y) returns 3
  → validDestinations.some(m => m.to === 3) // true!
  → executeMove(6, 3)
    → connection.invoke("MakeMove", 6, 3)
    → Server validates and executes
    → Server broadcasts GameUpdate with new state
    → Selection cleared, board re-rendered
```

## Point Coordinate Mapping

The `getPointAtPosition()` function handles the complex mapping between canvas coordinates and Backgammon point numbers:

- **Top row**: Points 13-24 (left to right: 13-18, BAR, 19-24)
- **Bottom row**: Points 12-1 (left to right: 12-7, BAR, 6-1)
- **Bar**: Point 0 (only clickable if you have checkers there)
- **Bear-off**: Points 25/0 (handled automatically by move validation)

## Deselection

Click anywhere outside a point to deselect:
- Clears `selectedChecker`
- Clears `validDestinations`
- Re-renders board without highlights

## Future Enhancements

- [ ] Add animation when pieces move
- [ ] Show die value on destination highlights
- [ ] Add sound effects for moves and hits
- [ ] Support drag-and-drop in addition to click
- [ ] Show preview ghost piece on hover

## Turn Control

### Undo Move

Players can undo their last move during their turn:

```javascript
undoMove()
  → connection.invoke("UndoMove")
  → Server calls Engine.UndoLastMove()
  → Reverses board state
  → Restores die to RemainingMoves
  → Broadcasts updated GameState
```

**Undo Logic (GameEngine.cs)**:
- Tracks all moves in `MoveHistory` list
- On undo, reverses the last move:
  - Regular move: moves checker back
  - Enter from bar: puts checker back on bar
  - Bear off: puts checker back on board
  - Hit: restores opponent's blot
- Restores the die value to `RemainingMoves`

**Button State**:
- Enabled when: your turn, has dice, and at least one move was made
- Disabled when: not your turn, no dice, or no moves made yet

### End Turn

Players must explicitly end their turn:

```javascript
endTurn()
  → connection.invoke("EndTurn")
  → Server calls Engine.EndTurn()
  → Clears RemainingMoves and MoveHistory
  → Switches to opponent
  → Broadcasts updated GameState
```

**Button State**:
- Enabled when: your turn and you have rolled dice
- Disabled when: not your turn or haven't rolled yet

This allows players to:
- Think about their move sequence
- Undo mistakes before committing
- Explicitly signal when they're done
