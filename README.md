# Backgammon .NET

[![Tests](https://github.com/garrett/Backgammon/actions/workflows/test.yml/badge.svg)](https://github.com/garrett/Backgammon/actions/workflows/test.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-13-239120?logo=csharp)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A complete implementation of the classic Backgammon board game in .NET, with support for console play, AI simulation, and **multiplayer via SignalR** with **MongoDB persistence**.

## Quick Start with .NET Aspire 🚀

Run the entire stack (MongoDB + Backend + Frontend) with one command:

```bash
cd Backgammon.AppHost
dotnet run
```

Opens Aspire Dashboard with observability for all services. See [ASPIRE_SETUP.md](ASPIRE_SETUP.md) for details.

## Project Structure

```
Backgammon/
├── Backgammon.AppHost/       # .NET Aspire orchestrator
├── Backgammon.ServiceDefaults/ # Shared Aspire configuration
├── Backgammon.Core/          # Game logic library
│   ├── Board.cs              # Game board with 24 points
│   ├── CheckerColor.cs       # Enum for White/Red
│   ├── Dice.cs               # Dice rolling logic
│   ├── DoublingCube.cs       # Doubling cube implementation
│   ├── GameEngine.cs         # Main game engine and rules
│   ├── Move.cs               # Represents a checker move
│   ├── Player.cs             # Player representation
│   └── Point.cs              # Point (triangle) on the board
├── Backgammon.Console/       # Console interface
│   └── Program.cs            # Text-based game interface
├── Backgammon.Web/           # SignalR multiplayer server
│   ├── Hubs/                 # SignalR hub for real-time game
│   ├── Services/             # Game session management + MongoDB
│   ├── Models/               # DTOs and game state
│   └── Program.cs            # Web server startup
├── Backgammon.WebClient/     # Web UI client (frontend)
│   ├── wwwroot/              # Static HTML/CSS/JS files
│   │   ├── index.html        # Main game interface
│   │   ├── styles.css        # Modern responsive UI
│   │   └── game.js           # SignalR client logic
│   └── Program.cs            # Static file server
├── TestClient/               # SignalR test client (console)
│   └── Program.cs            # Example .NET SignalR client
├── Backgammon.AI/            # AI simulation framework
│   ├── IBackgammonAI.cs      # AI player interface
│   ├── RandomAI.cs           # Random move AI
│   ├── GreedyAI.cs           # Greedy strategy AI
│   ├── AISimulator.cs        # Game simulation engine
│   └── Program.cs            # AI vs AI runner
└── Backgammon.Tests/         # xUnit test project
    └── GameEngineTests.cs    # Unit tests for game logic
```

## Features
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

**Quick Start (One Command):**
```bash
./start-web.sh
```
This script starts both the server and client automatically. Open `http://localhost:3000` in your browser!

**Manual Start:**

**Terminal 1 - Start SignalR Server:**
```bash
cd Backgammon.Web
dotnet run
```
Server runs on `http://localhost:5000`

**Terminal 2 - Start Web Client:**
```bash
cd Backgammon.WebClient
dotnet run
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
cd Backgammon.Web
dotnet run
# Server runs on http://localhost:5000

# Run AI simulations
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

The `Backgammon.Web` project provides a **SignalR server** that enables real-time multiplayer gameplay from any client platform:

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

See [Backgammon.Web/README.md](Backgammon.Web/README.md) for full documentation, client examples, and API reference.

### Quick Start - Multiplayer

```bash
# Terminal 1: Start server
cd Backgammon.Web
dotnet run

# Terminal 2: Test client
cd Backgammon.Web/TestClient
dotnet run
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

The project is split into two parts:

### Backgammon.Core (Game Logic)
- **Board**: Manages 24 points and checker positions
- **GameEngine**: Enforces all backgammon rules and validates moves
- **Player**: Tracks player state (checkers on bar, borne off)
- **Dice**: Handles random dice rolls
- **Move**: Represents checker movements
- **DoublingCube**: Manages stakes and doubling

### Backgammon.Console (UI)
- Text-based visualization of the board
- Interactive move selection
- Turn-by-turn gameplay

### Backgammon.AI (AI Framework)
- **IBackgammonAI**: Interface for creating AI players
- **RandomAI**: Baseline AI that makes random valid moves
- **GreedyAI**: Strategy-based AI that prioritizes bearing off and hitting
- **AISimulator**: Run games between two AIs and collect statistics
- See [Backgammon.AI/README.md](Backgammon.AI/README.md) for details on creating your own AI

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
- [ ] Network multiplayer
- [ ] Move suggestion/hint system
- [ ] Game replay and save/load functionality
- [ ] Match play with Crawford rule
- [ ] Statistics tracking
- [x] Undo/redo moves
- [ ] Optional rules (automatic doubles, beavers, Jacoby rule)

## Technical Details

- **Platform**: .NET 10.0
- **Language**: C# 13
- **Architecture**: Clean separation between game logic and presentation
- **Testing**: Easily testable due to separated concerns

## License

This is a demonstration project for learning purposes.

## Backgammon Rules Reference

The implementation follows standard backgammon rules. For detailed rules, see:
- Movement rules and initial setup
- Hitting and entering mechanics
- Bearing off conditions
- Doubling cube usage
- Gammon and backgammon scoring

---

**Enjoy playing Backgammon!** 🎲
