---
sidebar_position: 3
---

# Server Architecture

The `Backgammon.Server` project provides the real-time multiplayer backend.

## Technology Stack

- **ASP.NET Core** - Web framework
- **SignalR** - Real-time WebSocket communication
- **DynamoDB** - NoSQL database
- **HybridCache** - In-memory + Redis caching
- **JWT** - Authentication tokens

## Service Layer

### GameSessionManager

Manages active game sessions in memory.

```csharp
public interface IGameSessionManager
{
    GameSession CreateGame(string creatorId, string creatorName);
    GameSession? GetGame(string gameId);
    GameSession? GetGameByPlayer(string playerId);
    void RemoveGame(string gameId);
}
```

### GameSession

Holds game state and player connections.

```csharp
public class GameSession
{
    public string Id { get; }
    public GameEngine Engine { get; }

    // Multi-tab support: multiple connections per player
    public HashSet<string> WhiteConnections { get; }
    public HashSet<string> RedConnections { get; }

    public GameState GetState(string? connectionId);
}
```

### GameActionOrchestrator

Coordinates game actions with proper broadcasting.

```csharp
public interface IGameActionOrchestrator
{
    Task<MoveResult> MakeMoveAsync(GameSession session, string connectionId, int from, int to);
    Task RollDiceAsync(GameSession session, string connectionId);
    Task EndTurnAsync(GameSession session, string connectionId);
}
```

### MatchService

Manages match lifecycle.

```csharp
public interface IMatchService
{
    Task<Match> CreateMatchAsync(MatchConfig config, string creatorId);
    Task CompleteGameAsync(string matchId, GameResult result);
    Task<string> StartNextGameAsync(string matchId);
}
```

### EloRatingService

Calculates rating changes after matches.

```csharp
public interface IEloRatingService
{
    (int newWinnerRating, int newLoserRating) CalculateNewRatings(
        int winnerRating,
        int loserRating,
        bool isGammon);
}
```

## SignalR Hub

### IGameHub (Client → Server)

Methods clients can invoke:

```csharp
Task JoinGame(string? gameId);
Task RollDice();
Task MakeMove(int from, int to);
Task EndTurn();
Task OfferDouble();
Task AcceptDouble();
Task DeclineDouble();
// ... 60+ more methods
```

### IGameHubClient (Server → Client)

Events pushed to clients:

```csharp
Task GameUpdate(GameState gameState);
Task GameStart(GameState gameState);
Task GameOver(GameState gameState);
Task DoubleOffered(DoubleOfferDto offer);
Task MatchCompleted(MatchCompletedDto data);
// ... 20+ more events
```

## REST API Endpoints

Minimal API endpoints in `Program.cs`:

| Endpoint | Description |
|----------|-------------|
| `GET /health` | Health check |
| `GET /api/games` | List available games |
| `POST /api/auth/register` | User registration |
| `POST /api/auth/login` | User login |
| `GET /api/users/{id}` | Get user profile |
| `GET /api/friends` | Get friends list |
| `GET /api/themes` | Get board themes |

## DynamoDB Schema

Single-table design with composite keys:

| Entity | PK | SK |
|--------|----|----|
| User | `USER#{userId}` | `PROFILE` |
| Game | `GAME#{gameId}` | `METADATA` |
| Match | `MATCH#{matchId}` | `METADATA` |
| Friendship | `USER#{userId}` | `FRIEND#{status}#{friendId}` |

### Global Secondary Indexes

1. **GSI1**: Username lookups
2. **GSI2**: Email lookups
3. **GSI3**: Game/Match status queries
4. **GSI4**: Correspondence turn tracking

## Authentication Flow

1. Client sends JWT in query string: `/gamehub?access_token=xxx`
2. `JwtBearerEvents.OnMessageReceived` extracts token
3. Token validated against signing key
4. `ClaimsPrincipal` attached to connection context

Anonymous users are automatically created and receive a JWT for session continuity.
