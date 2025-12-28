# Backgammon.Server - SignalR Game Server

ASP.NET Core SignalR server for multiplayer Backgammon games. This is the **server-only** backend - use **Backgammon.WebClient** for the web UI.

## Architecture

- **SignalR Hub** (`GameHub`) - Real-time bidirectional communication
- **Game Session Manager** - Tracks active games and players
- **Backgammon.Core** - Server-authoritative game logic (validates all moves)
- **Multi-client support** - Web, console, mobile, desktop can all connect

## Quick Start

### Run the Server

```bash
cd Backgammon.Server
dotnet run
```

Server runs on `http://localhost:5000` by default.

### Run the Web Client (Separate Project)

```bash
cd ../Backgammon.WebClient
dotnet run
```

Then open `http://localhost:3000` in your browser.

### Test the Server

```bash
# Health check
curl http://localhost:5000/health

# Game statistics
curl http://localhost:5000/stats
```

## SignalR Hub Endpoints

**Hub URL**: `/gamehub`

### Server Methods (Client → Server)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `JoinGame` | `string? gameId` | Join specific game or use matchmaking (null) |
| `RollDice` | - | Roll dice to start turn |
| `GetValidSources` | - | Get list of points with movable checkers |
| `GetValidDestinations` | `int fromPoint` | Get valid moves from a specific point |
| `MakeMove` | `int from, int to` | Execute a move |
| `UndoMove` | - | Undo the last move made this turn |
| `EndTurn` | - | End current turn, switch players |
| `GetGameState` | - | Request current game state |
| `LeaveGame` | - | Leave current game |

### Client Methods (Server → Client)

| Method | Parameters | Description |
|--------|-----------|-------------|
| `GameStart` | `GameState` | Both players joined, game starting |
| `GameUpdate` | `GameState` | Game state changed |
| `DiceRolled` | `GameState` | Dice were rolled |
| `MoveMade` | `GameState` | Move was executed |
| `TurnEnded` | `GameState` | Turn ended |
| `GameOver` | `GameState` | Game completed |
| `WaitingForOpponent` | `string gameId` | Waiting for second player |
| `OpponentJoined` | - | Opponent connected |
| `OpponentLeft` | - | Opponent disconnected |
| `Error` | `string message` | Error occurred |

## Example Clients

### JavaScript/TypeScript (Web)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:5000/gamehub")
    .build();

// Handle server events
connection.on("GameStart", (gameState) => {
    console.log("Game started!", gameState);
});

connection.on("MoveMade", (gameState) => {
    console.log("Move made:", gameState);
    // Update UI with new board state
});

connection.on("Error", (message) => {
    console.error("Server error:", message);
});

// Start connection
await connection.start();

// Join matchmaking
await connection.invoke("JoinGame");

// Make a move
await connection.invoke("MakeMove", 24, 20);
```

### C# Console Client

```csharp
using Microsoft.AspNetCore.SignalR.Client;

var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/gamehub")
    .Build();

connection.On<GameState>("GameStart", (state) => {
    Console.WriteLine($"Game started! You are {state.CurrentPlayer}");
});

connection.On<GameState>("MoveMade", (state) => {
    // Render board
});

await connection.StartAsync();
await connection.InvokeAsync("JoinGame");
```

### Python Client

```python
from signalrcore.hub_connection_builder import HubConnectionBuilder

connection = HubConnectionBuilder()\
    .with_url("http://localhost:5000/gamehub")\
    .build()

connection.on("GameStart", lambda state: print(f"Game started: {state}"))
connection.on("MoveMade", lambda state: print(f"Move made: {state}"))

connection.start()
connection.send("JoinGame", [])
```

## Game Flow

1. **Connect** to `/gamehub` via SignalR
2. **JoinGame()** - either provide game ID or use matchmaking
3. **Wait** for `GameStart` event (when both players ready)
4. **On your turn**:
   - Call `RollDice()` if no remaining moves
   - Receive `DiceRolled` with valid moves
   - Call `MakeMove(from, to)` repeatedly
   - Call `EndTurn()` when done
5. **Receive** `GameOver` when game completes

## GameState Structure

```json
{
  "gameId": "abc123",
  "whitePlayerId": "connection-id-1",
  "redPlayerId": "connection-id-2",
  "currentPlayer": "White",
  "dice": [3, 4],
  "remainingMoves": [3, 4],
  "validMoves": [
    { "from": 24, "to": 21, "dieValue": 3, "isHit": false },
    { "from": 24, "to": 20, "dieValue": 4, "isHit": false }
  ],
  "board": [
    { "position": 1, "color": "Red", "count": 2 },
    { "position": 6, "color": "White", "count": 5 },
    ...
  ],
  "whiteCheckersOnBar": 0,
  "redCheckersOnBar": 0,
  "whiteBornOff": 0,
  "redBornOff": 0,
  "status": "InProgress",
  "winner": null,
  "winType": null
}
```

## Configuration

Edit [Program.cs](Program.cs):

- **CORS Policy**: Currently allows all origins (development only)
- **Cleanup Interval**: Inactive games removed every 5 minutes
- **Inactivity Timeout**: Games idle for 1+ hour are cleaned up

## Production Considerations

1. **Restrict CORS** - Only allow specific origins
2. **Add Authentication** - Validate user tokens
3. **Rate Limiting** - Prevent abuse
4. **Persistent Storage** - Save game state to database
5. **Scale Out** - Use SignalR backplane (Redis/Azure SignalR Service)
6. **Logging** - Configure structured logging
7. **Monitoring** - Add metrics and health checks

## Next Steps

- Create web frontend (React, Vue, Blazor)
- Add user authentication/authorization
- Implement game history and statistics
- Add chat functionality
- Create mobile apps using same server
- Add AI opponents as fallback
