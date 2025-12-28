# Database Integration Roadmap

## Current Implementation ✅

### Persistent Player IDs
- **Client-side**: localStorage stores unique player ID (`backgammon_player_id`)
- **Format**: `player_[timestamp]_[random]`
- **Survives**: Browser refresh, connection drops
- **Server**: Tracks player ID separately from SignalR connection ID

### Game Sessions
- **Unique Game IDs**: Each game has UUID
- **Player Tracking**: Separate `WhitePlayerId`/`RedPlayerId` from connection IDs
- **Reconnection**: Players can reconnect with new connections without resetting game

### URL Structure
Games are accessible via: `http://localhost:3000/?game=[game-id]`

## Phase 1: Database Schema (Next Step)

### Tables

```sql
-- Players table
CREATE TABLE Players (
    PlayerId VARCHAR(100) PRIMARY KEY,
    Username VARCHAR(50) UNIQUE,
    Email VARCHAR(255) UNIQUE,
    CreatedAt DATETIME NOT NULL,
    LastSeenAt DATETIME NOT NULL
);

-- Games table
CREATE TABLE Games (
    GameId VARCHAR(100) PRIMARY KEY,
    WhitePlayerId VARCHAR(100) REFERENCES Players(PlayerId),
    RedPlayerId VARCHAR(100) REFERENCES Players(PlayerId),
    Status VARCHAR(20) NOT NULL, -- WaitingForPlayer, InProgress, Completed
    CreatedAt DATETIME NOT NULL,
    CompletedAt DATETIME,
    WinnerColor VARCHAR(10),
    WinType VARCHAR(20), -- Normal, Gammon, Backgammon
    Stakes INT DEFAULT 1
);

-- Game states (for analysis/replay)
CREATE TABLE GameStates (
    StateId INT PRIMARY KEY AUTO_INCREMENT,
    GameId VARCHAR(100) REFERENCES Games(GameId),
    MoveNumber INT NOT NULL,
    BoardState JSON NOT NULL, -- Serialized board position
    CurrentPlayer VARCHAR(10), -- White or Red
    Dice JSON, -- [die1, die2]
    RemainingMoves JSON, -- [moves...]
    Timestamp DATETIME NOT NULL
);

-- Moves history (for PGN-style notation)
CREATE TABLE Moves (
    MoveId INT PRIMARY KEY AUTO_INCREMENT,
    GameId VARCHAR(100) REFERENCES Games(GameId),
    MoveNumber INT NOT NULL,
    PlayerColor VARCHAR(10) NOT NULL,
    FromPoint INT NOT NULL,
    ToPoint INT NOT NULL,
    DieValue INT NOT NULL,
    IsHit BOOLEAN DEFAULT FALSE,
    Timestamp DATETIME NOT NULL
);
```

## Phase 2: Entity Framework Integration

### Install Packages
```bash
dotnet add Backgammon.Server package Microsoft.EntityFrameworkCore
dotnet add Backgammon.Server package Microsoft.EntityFrameworkCore.Sqlite
dotnet add Backgammon.Server package Microsoft.EntityFrameworkCore.Design
```

### DbContext
```csharp
public class BackgammonDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<GameState> GameStates { get; set; }
    public DbSet<Move> Moves { get; set; }
}
```

## Phase 3: Migration Strategy

### Keep Current In-Memory for Active Games
- Active games remain in `GameSessionManager` (fast)
- Persist snapshots to database periodically
- Full save on game completion

### Background Persistence Service
```csharp
public class GamePersistenceService : BackgroundService
{
    // Every 30 seconds, save active game states
    // On game completion, save full history
}
```

## Phase 4: Lichess-Style Features

### Game URLs
- **Short URLs**: `https://backgammon.com/ABC123`
- **PGN Export**: Download move notation
- **Analysis Board**: Step through move history
- **Game Library**: Browse your past games

### Authentication (Optional)
- Sign in with Google/GitHub
- Link anonymous player ID to account
- Cross-device game access

### Statistics
- Win/loss record
- Rating system (ELO)
- Move accuracy analysis
- Opening preferences

## Phase 5: Advanced Features

### Computer Analysis
- Integrate existing AI from `Backgammon.AI`
- Analyze completed games
- Suggest better moves
- Calculate mistake severity

### Live Spectating
- Public game URLs
- Spectator mode (read-only)
- Tournament support

### Social Features
- Challenge friends
- Rating leaderboards
- Achievement system

## Migration Checklist

- [x] Implement persistent player IDs
- [x] Separate connection ID from player ID
- [ ] Design database schema
- [ ] Add Entity Framework
- [ ] Create DbContext and models
- [ ] Implement save on game end
- [ ] Add game history API endpoint
- [ ] Build game viewer/replay UI
- [ ] Add authentication (optional)
- [ ] Implement statistics dashboard

## File Structure (Future)

```
Backgammon.Data/
├── BackgammonDbContext.cs
├── Models/
│   ├── PlayerEntity.cs
│   ├── GameEntity.cs
│   ├── GameStateEntity.cs
│   └── MoveEntity.cs
├── Repositories/
│   ├── IGameRepository.cs
│   └── GameRepository.cs
└── Services/
    └── GamePersistenceService.cs

Backgammon.Server/
└── Controllers/
    ├── GameController.cs  (REST API)
    └── AnalysisController.cs
```

## Configuration

```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=backgammon.db"
  },
  "GamePersistence": {
    "SaveInterval": 30,
    "EnableAutoSave": true
  }
}
```

## Benefits of This Approach

1. **No Breaking Changes**: Current system continues to work
2. **Performance**: Active games stay in-memory
3. **Scalability**: Database handles historical data
4. **Analysis**: Full game history for review
5. **Recovery**: Can restore games from database if server crashes
