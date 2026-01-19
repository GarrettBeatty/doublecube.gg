---
sidebar_position: 1
---

# Multiplayer

DoubleCube.gg provides real-time multiplayer backgammon via SignalR WebSocket connections.

## Connection Flow

1. **Authentication**: Client receives JWT (anonymous or registered)
2. **Connection**: SignalR connects with token in query string
3. **Join Game**: Client invokes `JoinGame(gameId)`
4. **Real-time Updates**: Server pushes events to all connected clients

## Multi-Tab Support

A unique feature is full multi-tab support:

- Open the same game in multiple browser tabs
- All tabs stay synchronized
- Any tab can make moves
- Reconnection is seamless

### How It Works

Each player is tracked by a `HashSet<string>` of connection IDs:

```csharp
public class GameSession
{
    public HashSet<string> WhiteConnections { get; } = new();
    public HashSet<string> RedConnections { get; } = new();

    public void AddPlayer(string playerId, string connectionId)
    {
        // Same player, new connection = just add to set
        if (WhitePlayerId == playerId)
            WhiteConnections.Add(connectionId);
    }
}
```

When broadcasting updates, all connections receive the event:

```csharp
foreach (var connectionId in session.WhiteConnections)
{
    await Clients.Client(connectionId).SendAsync("GameUpdate", state);
}
```

## Game Lifecycle

### Creating a Game

```typescript
// Create via SignalR
await connection.invoke('CreateMatch', {
  targetScore: 5,
  opponentType: 'human',
  lobbyType: 'open'
});
```

### Joining a Game

```typescript
await connection.invoke('JoinGame', gameId);
```

### Making Moves

```typescript
// Roll dice first
await connection.invoke('RollDice');

// Make moves
await connection.invoke('MakeMove', fromPoint, toPoint);

// End turn
await connection.invoke('EndTurn');
```

## Reconnection Handling

SignalR automatically reconnects with exponential backoff:

```typescript
.withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
```

When reconnecting to a game:
1. Server detects reconnection (connection ID already in session)
2. Current game state sent to new connection
3. Game continues seamlessly

## Spectating

Users can watch games without participating:

```typescript
await connection.invoke('JoinGame', gameId); // As spectator
```

Spectators receive:
- `GameUpdate` events
- `GameOver` events
- No move capabilities

## Connection Events

### Client Events

| Event | Description |
|-------|-------------|
| `GameStart` | Both players joined, game begins |
| `GameUpdate` | Board state changed |
| `GameOver` | Game completed |
| `OpponentJoined` | Second player joined |
| `OpponentLeft` | Player disconnected |

### Handling Disconnection

```typescript
connection.onreconnecting(() => {
  showToast('Reconnecting...');
});

connection.onreconnected(() => {
  // Re-join current game
  await connection.invoke('JoinGame', currentGameId);
});

connection.onclose(() => {
  showToast('Connection lost');
});
```
