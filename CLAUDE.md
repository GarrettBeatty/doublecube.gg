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

# Run with Aspire (all-in-one: DynamoDB Local + Backend + Frontend)
cd Backgammon.AppHost && dotnet run

# Run console game
cd Backgammon.Console && dotnet run

# Run web multiplayer (manual)
cd Backgammon.Server && dotnet run      # Server on http://localhost:5000
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
- **Backgammon.Server** - SignalR multiplayer server with DynamoDB persistence. Contains `GameHub`, `GameSession`, `GameSessionManager`.
- **Backgammon.WebClient** - Static HTML/JS/CSS frontend with SignalR client
- **Backgammon.AI** - Pluggable AI framework. Implements `IBackgammonAI` interface with `RandomAI` and `GreedyAI`.
- **Backgammon.AppHost** - .NET Aspire orchestrator (manages DynamoDB Local, services)
- **Backgammon.Tests** / **Backgammon.IntegrationTests** - xUnit test projects

## Database: DynamoDB

The application uses **AWS DynamoDB** with a **single-table design** pattern for optimal performance and cost-efficiency.

### Local Development
- DynamoDB Local runs in a Docker container via Aspire
- Table auto-created on startup by `DynamoDbInitializer`
- Connection: http://localhost:8000
- Table name: `backgammon-local`

### Single-Table Design
All entities (Users, Games, Friendships) stored in one table with composite PK/SK:
- **Users**: `PK=USER#{userId}`, `SK=PROFILE`
- **Games**: `PK=GAME#{gameId}`, `SK=METADATA`
- **Player-Game Index**: `PK=USER#{playerId}`, `SK=GAME#{reversedTimestamp}#{gameId}`
- **Friendships**: `PK=USER#{userId}`, `SK=FRIEND#{status}#{friendUserId}`

### Global Secondary Indexes (GSIs)
1. **GSI1**: Username lookups - `GSI1PK=USERNAME#{normalized}`, `GSI1SK=PROFILE`
2. **GSI2**: Email lookups - `GSI2PK=EMAIL#{normalized}`, `GSI2SK=PROFILE`
3. **GSI3**: Game status queries - `GSI3PK=GAME_STATUS#{status}`, `GSI3SK={timestamp}`

### AWS Production Deployment
- Infrastructure managed by AWS CDK (see `infra/cdk/`)
- Deploy: `cd infra/cdk && cdk deploy`
- On-demand billing (pay-per-request)
- Point-in-time recovery enabled

## Core Domain Model

### Match Play Architecture

The application supports multi-game matches with proper match scoring and Crawford rule:

**Domain Layer** (`Backgammon.Core`):
- `Match` - Pure match logic, tracks score, Crawford state, game history
- `Game` - Individual game within a match (wraps GameEngine result)
- `GameResult` - Captures win type (Normal/Gammon/Backgammon) and points scored
- `MatchStatus` enum - InProgress, Completed, Abandoned

**Server Layer** (`Backgammon.Server`):
- `MatchService` - Orchestrates match lifecycle (create, start games, complete)
- `DynamoDbMatchRepository` - Persists matches and games to DynamoDB
- `Match` (server model) - Wraps `Core.Match` with server metadata (lobby status, opponent type, duration)

**Key Match Patterns**:
- Match created → First game starts automatically
- Game completes → `MatchService.CompleteGameAsync()` updates match score
- Crawford rule enforced automatically when score reaches targetScore-1
- Match completes when a player reaches targetScore

**Crawford Rule Implementation**:
```csharp
// Backgammon.Core.Match handles Crawford logic
match.RecordGameResult(gameResult);  // Auto-activates Crawford if needed
if (match.IsCrawfordGame) {
    // Doubling cube disabled for this game
}
```

**Match Lobby Flow**:
1. Player creates match lobby via `CreateMatchLobbyAsync()` (friend/AI/open lobby)
2. Opponent joins via `JoinMatchLobby()` SignalR method
3. Creator starts match via `StartMatchFromLobby()`
4. First game begins, players join game session

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
