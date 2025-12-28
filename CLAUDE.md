# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run with Aspire (all-in-one: MongoDB + Backend + Frontend)
cd Backgammon.AppHost && dotnet run

# Run console game
cd Backgammon.Console && dotnet run

# Run web multiplayer (manual)
cd Backgammon.Web && dotnet run      # Server on http://localhost:5000
cd Backgammon.WebClient && dotnet run # Client on http://localhost:3000

# Quick start web (script)
./start-web.sh

# Run AI simulations
cd Backgammon.AI && dotnet run
```

## Architecture

**Multi-project solution** for a Backgammon game with console UI, web multiplayer, and AI framework:

- **Backgammon.Core** - Pure game logic library (no dependencies). Contains `GameEngine`, `Board`, `Player`, `Dice`, `Move`, `DoublingCube`.
- **Backgammon.Console** - Text-based UI using Spectre.Console
- **Backgammon.Web** - SignalR multiplayer server with MongoDB persistence. Contains `GameHub`, `GameSession`, `GameSessionManager`.
- **Backgammon.WebClient** - Static HTML/JS/CSS frontend with SignalR client
- **Backgammon.AI** - Pluggable AI framework. Implements `IBackgammonAI` interface with `RandomAI` and `GreedyAI`.
- **Backgammon.AppHost** - .NET Aspire orchestrator (manages MongoDB, services)
- **Backgammon.Tests** / **Backgammon.IntegrationTests** - xUnit test projects

## Core Domain Model

### Board Representation
- 24 points (positions 1-24) as `Point[]` array
- Point 0 = bar, Point 25 = bear off
- **White moves 24→1** (descending), **Red moves 1→24** (ascending)
- Home boards: White = points 1-6, Red = points 19-24

### GameEngine Lifecycle
```csharp
1. Check game.RemainingMoves.Count > 0
2. If 0, call game.RollDice()
3. Call game.GetValidMoves() for legal moves
4. game.ExecuteMove(move) - automatically updates RemainingMoves
5. game.EndTurn() to switch players
```

**Never manually manipulate `RemainingMoves`** - `ExecuteMove()` manages it.

### Key Rules Enforced by Engine
- Bar priority: checkers on bar must enter first
- Bearing off: only when all checkers in home board
- Forced moves: if only one die can be used, must use larger value
- Doubles: roll same number = 4 moves

## Testing Patterns

Tests directly manipulate board state for scenarios:
```csharp
var game = new GameEngine();
game.StartNewGame();
game.Board.GetPoint(1).Checkers.Clear();
game.Board.GetPoint(1).AddChecker(CheckerColor.White);
game.Dice.SetDice(3, 4);
game.RemainingMoves.Clear();
game.RemainingMoves.AddRange(game.Dice.GetMoves());
```

## AI Implementation

Implement `IBackgammonAI.ChooseMoves(GameEngine engine)`:
```csharp
while (engine.RemainingMoves.Count > 0) {
    var validMoves = engine.GetValidMoves();
    if (validMoves.Count == 0) break;
    var move = /* select */;
    engine.ExecuteMove(move);
}
```

`GetValidMoves()` returns new list each call after `ExecuteMove()` changes state.

## Aspire Development

When working with Aspire orchestration:
1. Run with `aspire run` from Backgammon.AppHost
2. Changes to AppHost.cs require restart
3. Use Aspire MCP tools to check resource status and debug
4. Avoid persistent containers during development
