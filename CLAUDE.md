# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Tool Preferences

**IMPORTANT: Always use Serena MCP tools when possible for code exploration and manipulation.**

Serena provides semantic, symbol-aware operations that are more efficient and accurate than text-based tools:

- **Code Exploration**: Use `get_symbols_overview`, `find_symbol`, `search_for_pattern` instead of Grep/Read for understanding code
- **Code Navigation**: Use `find_referencing_symbols` to understand dependencies and usage
- **Code Editing**: Use `replace_symbol_body`, `insert_after_symbol`, `insert_before_symbol` for precise code changes
- **Refactoring**: Use `rename_symbol` for safe, project-wide renames

Serena has been initialized with project-specific memories about architecture, conventions, and workflows.

## Code Quality & Style Guidelines

### StyleCop Analyzers (C# Backend)

This project enforces StyleCop code quality rules. **Always follow these guidelines when writing C# code:**

#### Critical StyleCop Rules

1. **SA1402: File may only contain a single type**
   - Each file must contain only ONE class, interface, enum, or struct
   - Exception: Nested types within a parent type are allowed
   - ❌ BAD: Two classes in one file
   - ✅ GOOD: One class per file

2. **SA1649: File name should match first type name**
   - File name must exactly match the class/type name
   - Example: Class `UserRepository` → File `UserRepository.cs`
   - Case-sensitive match required

3. **SA1633: File should have header**
   - Use XML documentation comments (`///`) for all public types and members
   - Document purpose, parameters, return values, and exceptions

4. **SA1200: Using directives should be placed correctly**
   - Place all `using` statements outside the namespace
   - Order: System namespaces first, then third-party, then project namespaces

5. **SA1309: Field names should not begin with underscore**
   - This rule is typically **disabled** in this project (private fields use `_fieldName` convention)
   - If enabled, would require private fields without underscores

#### Common StyleCop Patterns in This Project

```csharp
// ✅ GOOD: Follows all StyleCop rules
using System;
using Microsoft.Extensions.Logging;
using Backgammon.Server.Models;

namespace Backgammon.Server.Services;

/// <summary>
/// Manages user authentication and authorization.
/// </summary>
public class AuthService
{
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }
}
```

#### When Creating New Files

1. **One class per file** - Create separate files for each class
2. **Match file name to class name** exactly
3. **Add XML documentation** for all public members
4. **Use proper namespace** matching the folder structure
5. **Order usings** correctly (System first, then alphabetically)

#### Quick StyleCop Checklist

Before committing C# code:
- [ ] One type per file
- [ ] File name matches class name exactly
- [ ] XML documentation on public types/members
- [ ] Usings are outside namespace and ordered correctly
- [ ] Private fields use `_camelCase` convention
- [ ] No unused using statements
- [ ] Proper indentation (4 spaces, not tabs)

### Frontend Code Quality (TypeScript/React)

- Follow ESLint rules (enforced by build)
- No unused variables or imports
- Avoid constant expressions in conditionals (`false &&`, `true ||`)
- Use TypeScript strict mode patterns
- Prefer functional components with hooks

## Build & Run Commands

```bash
# Build backend
dotnet build

# Build frontend
cd Backgammon.WebClient && pnpm build

# Regenerate TypedSignalR client (after changing IGameHub or IGameHubClient interfaces)
cd Backgammon.WebClient && pnpm generate:signalr

# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run with Aspire (all-in-one: DynamoDB Local + Redis + Backend + Frontend)
cd Backgammon.AppHost && dotnet run

# Run console game
cd Backgammon.Console && dotnet run

# Run web multiplayer (manual)
cd Backgammon.Server && dotnet run            # Server on http://localhost:5000
cd Backgammon.WebClient && pnpm dev           # Client dev server on http://localhost:3000

# Quick start web (script)
./start-web.sh

# Run AI simulations
cd Backgammon.AI && dotnet run

# Build/run documentation site
pnpm docs:dev                                  # Start docs dev server
pnpm docs:build                                # Build docs for production
```

## Architecture

**Multi-project solution** for a Backgammon game with console UI, web multiplayer, and AI framework:

- **Backgammon.Core** - Pure game logic library (no dependencies). Contains `GameEngine`, `Board`, `Player`, `Dice`, `Move`, `DoublingCube`, `Match`, `GameHistory`, `TurnSnapshot`.
- **Backgammon.Console** - Text-based UI using Spectre.Console
- **Backgammon.Server** - SignalR multiplayer server with DynamoDB persistence. Contains `GameHub`, `GameSession`, `GameSessionManager`, `MatchService`, `EloRatingService`, `CorrespondenceGameService`, `DailyPuzzleService`, `ChatService`, `AnalysisService`.
- **Backgammon.WebClient** - React + TypeScript + Vite frontend with real-time SignalR communication. Uses shadcn/ui, TailwindCSS, Zustand for state management.
- **Backgammon.AI** - Pluggable AI framework. Implements `IBackgammonAI` interface with `RandomAI`, `GreedyAI`, and heuristic-based bots.
- **Backgammon.Analysis** - Position evaluation and analysis. Integrates with GNU Backgammon (`GnubgEvaluator`) and provides `HeuristicEvaluator`.
- **Backgammon.Plugins** - Plugin registry for bots and evaluators. Provides `IPluginRegistry`, `BotMetadata`, `EvaluatorMetadata`.
- **Backgammon.AppHost** - .NET Aspire orchestrator (manages DynamoDB Local, Redis, services)
- **Backgammon.ServiceDefaults** - Shared Aspire configuration for observability and health checks
- **Backgammon.Tests** - xUnit test project
- **Backgammon.IntegrationTests** - Integration test suite

## Database: DynamoDB

The application uses **AWS DynamoDB** with a **single-table design** pattern for optimal performance and cost-efficiency.

### Local Development
- DynamoDB Local runs in a Docker container via Aspire
- Table auto-created on startup by `DynamoDbInitializer`
- Connection: http://localhost:8000
- Table name: `backgammon-local`

### Single-Table Design
All entities stored in one table with composite PK/SK:
- **Users**: `PK=USER#{userId}`, `SK=PROFILE`
- **Games**: `PK=GAME#{gameId}`, `SK=METADATA`
- **Player-Game Index**: `PK=USER#{playerId}`, `SK=GAME#{reversedTimestamp}#{gameId}`
- **Matches**: `PK=MATCH#{matchId}`, `SK=METADATA`
- **Player-Match Index**: `PK=USER#{playerId}`, `SK=MATCH#{reversedTimestamp}#{matchId}`
- **Friendships**: `PK=USER#{userId}`, `SK=FRIEND#{status}#{friendUserId}`
- **Themes**: `PK=THEME#{themeId}`, `SK=PROFILE`
- **Puzzles**: `PK=PUZZLE#{puzzleId}`, `SK=METADATA`
- **Match Chat**: `PK=MATCH#{matchId}`, `SK=MESSAGE#{timestamp}#{messageId}`

### Global Secondary Indexes (GSIs)
1. **GSI1**: Username lookups - `GSI1PK=USERNAME#{normalized}`, `GSI1SK=PROFILE`
2. **GSI2**: Email lookups - `GSI2PK=EMAIL#{normalized}`, `GSI2SK=PROFILE`
3. **GSI3**: Game/Match status queries - `GSI3PK=GAME_STATUS#{status}`, `GSI3SK={timestamp}`
4. **GSI4**: Correspondence "My Turn" index - `GSI4PK=PLAYER#{playerId}`, `GSI4SK=CORRESPONDENCE#{reversed_timestamp}`

### AWS Production Deployment
- Infrastructure managed by AWS CDK (see `infra/cdk/`)
- Deploy: `cd infra/cdk && cdk deploy`
- On-demand billing (pay-per-request)
- Point-in-time recovery enabled

## Frontend Architecture (WebClient)

**Tech Stack:**
- **React 18** - UI framework with functional components and hooks
- **TypeScript 5.3** - Type-safe development
- **Vite 7** - Fast build tool and dev server
- **shadcn/ui** - Accessible component library built on Radix UI
- **TailwindCSS 3** - Utility-first CSS framework
- **Zustand** - Lightweight state management
- **SignalR (@microsoft/signalr 8.0)** - Real-time WebSocket communication
- **React Router 6** - Client-side routing
- **Recharts** - Data visualization for statistics

**Project Structure:**
```
Backgammon.WebClient/
├── src/                      # Source code (NOT committed build outputs)
│   ├── components/           # React components
│   │   ├── board/           # Board rendering (BoardSVG, checkers)
│   │   ├── game/            # Game components (GameStatus, PlayerCard, MoveList)
│   │   ├── puzzle/          # Daily puzzle (DailyPuzzleBoard, PuzzleStats)
│   │   ├── players/         # Player list and profiles
│   │   ├── friends/         # Friend list and social features
│   │   ├── home/            # Home page components
│   │   ├── themes/          # Theme selector and customization
│   │   ├── layout/          # Layout components
│   │   ├── modals/          # Modal dialogs
│   │   └── ui/              # shadcn/ui components (30+)
│   ├── contexts/            # React contexts (SignalRContext, AuthContext, MatchContext)
│   ├── hooks/               # Custom React hooks (useSignalREvents)
│   ├── lib/                 # Utility libraries
│   ├── pages/               # Route pages (17 pages including Analysis, Puzzle, Profile)
│   ├── services/            # Service layer (signalr, auth, api, theme, audio)
│   ├── stores/              # Zustand stores (game, match, analysis, puzzle, theme, chat, etc.)
│   ├── styles/              # Global styles
│   ├── types/               # TypeScript type definitions (includes generated types)
│   ├── utils/               # Utility functions
│   └── main.tsx             # App entry point
├── wwwroot/                 # Build output directory (gitignored)
├── index.html               # Vite HTML template
├── vite.config.ts           # Vite configuration
├── tailwind.config.js       # TailwindCSS configuration
├── components.json          # shadcn/ui configuration
└── package.json             # pnpm dependencies
```

**Key Patterns:**

1. **Multi-Tab Support** - Players can open the same game in multiple browser tabs. The server tracks multiple connections per player using `HashSet<string>` for connection IDs. All tabs receive real-time updates and can make moves.

2. **SignalR Event Handling** - `useSignalREvents` hook registers event handlers once on mount and uses refs to avoid constant cleanup/re-registration. Events are filtered by game ID to prevent cross-game interference.

3. **State Management** - Multiple Zustand stores for different concerns:
   - `gameStore` - Current game state and player color
   - `matchStore` - Match-level state (scores, Crawford)
   - `analysisStore` - Position analysis results
   - `puzzleStore` - Daily puzzle state
   - `themeStore` - Theme preferences
   - `chatStore` - Chat messages
   - `boardInteractionStore` - User interactions (selected point, drag state)

4. **Real-time Communication Flow:**
   - Client → `invoke(HubMethods.MakeMove, ...)` → Server
   - Server → `SendAsync("GameUpdate", state)` → All connected tabs
   - `useSignalREvents` → Updates Zustand store → React re-renders

5. **Connection Lifecycle:**
   - Page loads → SignalR connects → Sets `isConnected = true`
   - GamePage waits for `isConnected` before calling `JoinGame`
   - On unmount, cleanup effect calls `LeaveGame`
   - Prevents race conditions where join attempts before connection ready

6. **Build Process:**
   - Development: `npm run dev` → Vite dev server with HMR
   - Production: `npm run build` → TypeScript compile + Vite bundle → `wwwroot/`
   - Output is gitignored, generated fresh on deploy

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
2. Opponent joins via `JoinMatch()` SignalR method
3. Creator starts match via `StartMatchFromLobby()`
4. First game begins, players join game session

### Multi-Tab Connection Management

The server supports **multiple browser tabs** per player in the same game:

**GameSession Architecture:**
- `_whiteConnections: HashSet<string>` - Tracks all White player's connections
- `_redConnections: HashSet<string>` - Tracks all Red player's connections
- `WhiteConnectionId` / `RedConnectionId` - Legacy properties (return first connection for backward compatibility)
- `GetPlayerColor(connectionId)` - Checks if connection belongs to a player
- `AddPlayer(playerId, connectionId)` - Adds connection to player's set (multiple connections allowed)

**Broadcast Pattern:**
All game update methods iterate through all connections:
```csharp
foreach (var connectionId in session.WhiteConnections)
{
    var state = session.GetState(connectionId);
    await _hubContext.Clients.Client(connectionId).SendAsync("GameUpdate", state);
}
```

**Reconnection Detection:**
When a player joins an already-in-progress game:
1. Check if `session.Engine.GameStarted` is true
2. If yes → Reconnection → Send current state to new connection only
3. If no → New game → Broadcast GameStart to all connections

This allows players to:
- Open same game in multiple tabs
- Make moves from any tab
- See real-time updates in all tabs
- Seamlessly reconnect after network issues

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

## ELO Rating System

The server implements an ELO-based rating system for player skill tracking:

**Key Components:**
- `EloRatingService` - Calculates rating changes after matches
- `PlayerStatsService` - Tracks wins, losses, rating history
- Rating stored per user in DynamoDB

**Rating Flow:**
1. Match completes → `EloRatingService.CalculateNewRatings()`
2. Ratings updated based on outcome and opponent strength
3. Stats persisted via `PlayerStatsService`

See `docs/elo-rating-implementation-plan.md` for detailed algorithm.

## Position Analysis (Backgammon.Analysis)

The Analysis project provides move evaluation and suggestions:

**Evaluators:**
- `GnubgEvaluator` - Uses GNU Backgammon for expert-level analysis
- `HeuristicEvaluator` - Fast built-in position scoring

**Models:**
- `PositionEvaluation` - Win/gammon/backgammon probabilities
- `MoveSequenceEvaluation` - Ranked move suggestions
- `CubeDecision` - Double/take/pass recommendations

**Integration:**
The server's `AnalysisService` exposes evaluation through SignalR for real-time hints.

See `docs/GNUBG_SETUP.md` for GNU Backgammon configuration.

## Correspondence Games

The server supports turn-based asynchronous games:

**Key Components:**
- `CorrespondenceGameService` - Manages correspondence match lifecycle
- `CorrespondenceTimeoutService` - Background service checking for expired turns
- `GSI4` - "My Turn" index for efficient turn-based queries

**Flow:**
1. Player creates correspondence match via `CreateCorrespondenceMatch()`
2. Game state persisted to DynamoDB after each turn
3. Opponent notified via `CorrespondenceTurnNotification` event
4. `GetCorrespondenceGames()` returns games awaiting player's turn

## Daily Puzzle System

Daily puzzles using GNU Backgammon for position generation:

**Components:**
- `DailyPuzzleService` - Retrieves/validates daily puzzles
- `DailyPuzzleGenerationService` - Background service generating puzzles
- `RandomPositionGenerator` - Creates random positions for evaluation
- `DynamoDbPuzzleRepository` - Puzzle persistence

**Puzzle Flow:**
1. Background service generates puzzle at configured time
2. Position evaluated by GNU Backgammon for optimal moves
3. Players solve via `SubmitPuzzleAnswer()` with move comparison
4. Streak tracking via `PuzzleStreakInfo`

## Caching Strategy

HybridCache with two-tier architecture:

**Configuration** (`CacheSettings`):
```json
{
  "CacheSettings": {
    "UserProfile": { "Expiration": "00:05:00", "LocalCacheExpiration": "00:01:00" },
    "PlayerStats": { "Expiration": "00:15:00", "LocalCacheExpiration": "00:03:00" },
    "FriendsList": { "Expiration": "00:05:00", "LocalCacheExpiration": "00:01:00" }
  }
}
```

**Layers:**
- **L1 (Local)**: In-memory cache per server instance
- **L2 (Distributed)**: Redis for cross-server sharing

**Usage Pattern:**
```csharp
var user = await _cache.GetOrCreateAsync(
    $"user:{userId}",
    async ct => await _userRepo.GetByUserIdAsync(userId),
    new HybridCacheEntryOptions { ... },
    tags: [$"player:{userId}"]);
```

## Plugin System

Bots and evaluators registered via plugin architecture:

**Components:**
- `IPluginRegistry` - Central registry for plugins
- `BotMetadata` / `EvaluatorMetadata` - Plugin descriptors
- `IBotResolver` - Creates bot instances by ID

**Registration:**
```csharp
// In Program.cs
builder.Services.AddBackgammonPlugins(builder.Configuration);
builder.Services.AddStandardBots();      // Random, Greedy
builder.Services.AddHeuristicBot();      // Heuristic evaluator
builder.Services.AddAnalysisPlugins();   // GNU Backgammon (if available)
```

**Available Bots:**
- `random` - Random valid moves (~800 ELO)
- `greedy` - Prioritizes bearing off/hitting (~1100 ELO)
- `heuristic` - Position evaluation (~1300 ELO)
- `gnubg` - GNU Backgammon neural network (~1800+ ELO)

## Feature Flags

Runtime feature toggles in configuration:

```json
{
  "Features": {
    "BotGamesEnabled": false,
    "MaxBotGames": 5,
    "BotGameRestartDelaySeconds": 30
  }
}
```

Accessed via `IOptions<FeatureFlags>` injection.

## Aspire Development

When working with Aspire orchestration:
1. Run with `dotnet run` from Backgammon.AppHost directory
2. Changes to AppHost Program.cs require restart
3. Use Aspire MCP tools to check resource status and debug
4. Avoid persistent containers during development
5. See `AGENTS.md` for Aspire-specific agent instructions
