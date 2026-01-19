# DoubleCube.gg

[![Tests](https://github.com/garrett/doublecube.gg/actions/workflows/test.yml/badge.svg)](https://github.com/garrett/doublecube.gg/actions/workflows/test.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](LICENSE)

🎲 **[Play online at doublecube.gg](https://doublecube.gg)** 🎲

A complete implementation of the classic Backgammon board game in .NET, with support for console play, AI simulation, and **online multiplayer via SignalR** with **DynamoDB persistence**.

## Quick Start with .NET Aspire 🚀

Run the entire stack (DynamoDB Local + Backend + Frontend) with one command:

```bash
cd Backgammon.AppHost
dotnet run
```

Opens Aspire Dashboard with observability for all services.

## Project Structure

```
doublecube.gg/
├── Backgammon.AppHost/         # .NET Aspire orchestrator
├── Backgammon.ServiceDefaults/ # Shared Aspire configuration
├── Backgammon.Core/            # Game logic library
│   ├── Board.cs                # Game board with 24 points
│   ├── Match.cs                # Match play with Crawford rule
│   ├── GameEngine.cs           # Main game engine and rules
│   ├── DoublingCube.cs         # Doubling cube implementation
│   ├── Dice.cs                 # Dice rolling logic
│   ├── Move.cs                 # Represents a checker move
│   └── Player.cs               # Player representation
├── Backgammon.Console/         # Console interface with Spectre.Console
├── Backgammon.Server/          # SignalR multiplayer server
│   ├── Hubs/                   # SignalR hub for real-time game
│   ├── Services/               # Game/Match/ELO services + DynamoDB
│   ├── Models/                 # DTOs and game state
│   └── Program.cs              # Web server startup
├── Backgammon.WebClient/       # React + TypeScript + Vite frontend
│   ├── src/                    # Source code
│   │   ├── components/         # React components (game, ui, modals)
│   │   ├── pages/              # Route pages (Home, Game)
│   │   ├── services/           # SignalR service layer
│   │   ├── stores/             # Zustand state management
│   │   └── types/              # TypeScript definitions
│   └── package.json            # npm dependencies
├── Backgammon.AI/              # AI simulation framework
│   ├── IBackgammonAI.cs        # AI player interface
│   ├── RandomAI.cs             # Random move AI
│   ├── GreedyAI.cs             # Greedy strategy AI
│   └── AISimulator.cs          # Game simulation engine
├── Backgammon.Analysis/        # Position analysis (GNU Backgammon integration)
│   ├── Gnubg/                  # GNU Backgammon evaluator
│   ├── Models/                 # Evaluation models
│   └── HeuristicEvaluator.cs   # Built-in heuristic evaluator
├── Backgammon.Plugins/         # Plugin system for bots and evaluators
├── Backgammon.Tests/           # xUnit test project
├── Backgammon.IntegrationTests/ # Integration tests
├── infra/                      # AWS CDK infrastructure
├── documentation/              # Docusaurus documentation site
└── docs/                       # Additional documentation
```

## Features

### Core Game
- ✅ Standard board setup with initial checker positions
- ✅ Dice rolling and turn management
- ✅ Checker movement in correct directions
- ✅ Hitting and entering from the bar
- ✅ Bearing off when all checkers in home board
- ✅ Doubles (roll same number = 4 moves)
- ✅ Move validation (must use larger die if only one can be played)
- ✅ Forced entry when checkers on bar
- ✅ Doubling cube for stakes
- ✅ Gammon and Backgammon detection
- ✅ Win condition checking

### Multiplayer & Social
- ✅ Real-time multiplayer via SignalR
- ✅ Correspondence games (asynchronous play)
- ✅ Friend system with add/remove/block
- ✅ Match chat with spectator support
- ✅ ELO rating system with leaderboards

### Analysis & AI
- ✅ GNU Backgammon integration for expert analysis
- ✅ Daily puzzles with difficulty levels
- ✅ Move hints and position evaluation
- ✅ AI opponents (Random, Greedy, Plugin-based)

### Customization
- ✅ Board themes and customization
- ✅ Match play with Crawford rule

### Game Rules Overview

**Objective**: Move all 15 checkers into your home board and bear them off before your opponent.

**Setup**: 
- White starts at point 1, Red starts at point 24
- Initial position: 2 on 24-point, 5 on 13-point, 3 on 8-point, 5 on 6-point

**Movement**:
- White moves from high numbers (24) to low numbers (1)
- Red moves from low numbers (1) to high numbers (24)
- Checkers can only land on open points (not occupied by 2+ opponent checkers)
- Must use both dice if possible; if not, must use larger number

**Special Rules**:
- **Blot**: Single checker can be hit and sent to the bar
- **Bar Entry**: Must enter checkers from bar before making other moves
- **Bearing Off**: Can only bear off when all checkers are in home board
- **Doubles**: Rolling same number gives 4 moves instead of 2

**Winning**:
- **Normal Win**: Opponent has borne off at least 1 checker (1x stakes)
- **Gammon**: Opponent has borne off 0 checkers (2x stakes)
- **Backgammon**: Opponent has 0 borne off AND has checker on bar or in winner's home (3x stakes)

## How to Run

### 🌐 Web Multiplayer (Recommended)

**Using Aspire (Recommended):**
```bash
cd Backgammon.AppHost
dotnet run
```
This starts DynamoDB Local, the SignalR server, and the frontend automatically with observability.

**Manual Start:**

**Terminal 1 - Start SignalR Server:**
```bash
cd Backgammon.Server
dotnet run
```
Server runs on `http://localhost:5000`

**Terminal 2 - Start Web Client:**
```bash
cd Backgammon.WebClient
pnpm dev
```
Web UI runs on `http://localhost:3000`

Open `http://localhost:3000` in two browser windows/tabs to play against yourself or share the link!

### 🎮 Console Application (Local Play)

```bash
cd Backgammon.Console
dotnet run
```

### 🤖 AI Simulation

```bash
cd Backgammon.AI
dotnet run
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal
```

## Multiplayer Support 🌐

The `Backgammon.Server` project provides a **SignalR server** that enables real-time multiplayer gameplay from any client platform:

- ✅ **Web browsers** (JavaScript/TypeScript)
- ✅ **Mobile apps** (iOS, Android via SignalR clients)
- ✅ **Desktop apps** (.NET, Python, Java)
- ✅ **Console clients** (included test client)

**Key Features:**
- Server-authoritative game logic (prevents cheating)
- Automatic matchmaking or private games
- Real-time bidirectional communication
- Supports multiple simultaneous games
- Works with any SignalR-compatible client

See [Backgammon.Server/README.md](Backgammon.Server/README.md) for full documentation, client examples, and API reference.

### Quick Start - Multiplayer

```bash
# Option 1: Using Aspire (starts everything)
cd Backgammon.AppHost && dotnet run

# Option 2: Manual (two terminals)
# Terminal 1: Start server
cd Backgammon.Server && dotnet run

# Terminal 2: Start web client
cd Backgammon.WebClient && pnpm dev
```

### Game Controls (Console)

1. Press Enter to roll dice at the start of your turn
2. Choose from the list of valid moves by entering the number
3. You can end your turn early if you can't or don't want to use remaining dice

## Code Examples

### Starting a New Game

```csharp
var game = new GameEngine("Player 1", "Player 2");
game.StartNewGame();
```

### Making Moves

```csharp
// Roll dice
game.RollDice();

// Get all valid moves
var validMoves = game.GetValidMoves();

// Execute a move
var move = validMoves[0];
game.ExecuteMove(move);

// End turn
if (game.RemainingMoves.Count == 0)
{
    game.EndTurn();
}
```

### Checking Game State

```csharp
// Check if game is over
if (game.GameOver)
{
    Console.WriteLine($"{game.Winner.Name} wins!");
    int points = game.GetGameResult();
    Console.WriteLine($"Points: {points}");
}
```

## Architecture

### Backgammon.Core (Game Logic)
- **Board**: Manages 24 points and checker positions
- **GameEngine**: Enforces all backgammon rules and validates moves
- **Match**: Multi-game match play with Crawford rule support
- **Player**: Tracks player state (checkers on bar, borne off)
- **Dice**: Handles random dice rolls
- **Move**: Represents checker movements
- **DoublingCube**: Manages stakes and doubling

### Backgammon.Server (Multiplayer Backend)
- **GameHub**: SignalR hub for real-time game communication
- **MatchService**: Orchestrates match lifecycle and scoring
- **EloRatingService**: Player skill rating system
- **DynamoDB Repositories**: Persistent storage for users, games, matches
- See [Backgammon.Server/README.md](Backgammon.Server/README.md) for API documentation

### Backgammon.WebClient (React Frontend)
- **React 18** with TypeScript and Vite
- **shadcn/ui** + TailwindCSS for modern UI
- **Zustand** for state management
- **SignalR** client for real-time updates

### Backgammon.Console (UI)
- Text-based visualization using Spectre.Console
- Interactive move selection
- Turn-by-turn gameplay

### Backgammon.AI (AI Framework)
- **IBackgammonAI**: Interface for creating AI players
- **RandomAI**: Baseline AI that makes random valid moves
- **GreedyAI**: Strategy-based AI that prioritizes bearing off and hitting
- **AISimulator**: Run games between two AIs and collect statistics
- See [Backgammon.AI/README.md](Backgammon.AI/README.md) for details on creating your own AI

### Backgammon.Analysis (Position Evaluation)
- **GnubgEvaluator**: Integration with GNU Backgammon for expert analysis
- **HeuristicEvaluator**: Built-in position scoring
- Move analysis and best move suggestions

### Backgammon.Plugins (Plugin System)
- **IPlugin**: Interface for creating custom bots and evaluators
- Hot-reload plugin support
- Sandboxed plugin execution

## Getting Started

### Play Human vs Human
```bash
cd Backgammon.Console
dotnet run
```

### Run AI Simulations
```bash
cd Backgammon.AI
dotnet run
```
Choose your AI matchup and number of games to simulate. The simulator will show win percentages and an example game.

### Run Tests
```bash
cd Backgammon.Tests
dotnet test
```

## Future Enhancements

Potential additions to the project:

- [ ] GUI version (WPF, Blazor, or MAUI)
- [x] AI opponent framework
- [ ] Advanced AI strategies (Monte Carlo, neural networks)
- [x] Network multiplayer (SignalR + React)
- [x] Move suggestion/hint system (GNU Backgammon integration)
- [x] Game replay and save/load functionality (SGF format)
- [x] Match play with Crawford rule
- [x] Statistics tracking (ELO rating system)
- [x] Undo/redo moves
- [x] Friend system and social features
- [x] Correspondence games (asynchronous play)
- [x] Daily puzzles
- [x] Board themes and customization
- [ ] Optional rules (automatic doubles, beavers, Jacoby rule)
- [ ] Tournament support

## Technical Details

- **Platform**: .NET 10.0
- **Language**: C# 13
- **Architecture**: Clean separation between game logic and presentation
- **Testing**: Easily testable due to separated concerns

## License

This project is licensed under the GNU Affero General Public License v3.0 (AGPL-3.0) - see the [LICENSE](LICENSE) file for details.

## Backgammon Rules Reference

The implementation follows standard backgammon rules. For detailed rules, see:
- Movement rules and initial setup
- Hitting and entering mechanics
- Bearing off conditions
- Doubling cube usage
- Gammon and backgammon scoring

---

**Enjoy playing at [doublecube.gg](https://doublecube.gg)!** 🎲
