# MongoDB Integration - Setup Guide

## Architecture Overview

This project uses the **Repository Pattern** to abstract database operations, making it easy to swap between MongoDB, DynamoDB, CosmosDB, or any other storage provider without changing business logic.

### Components

```
IGameRepository (interface)
    ↓
MongoGameRepository (MongoDB implementation)
    ↓
MongoDB (persistent storage)
```

**Active games**: Stored in-memory via `GameSessionManager`  
**Completed games**: Persisted to MongoDB automatically when game ends

## MongoDB Setup

### 1. Install MongoDB Locally

**macOS (Homebrew):**
```bash
brew tap mongodb/brew
brew install mongodb-community
brew services start mongodb-community
```

**Docker:**
```bash
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

**Verify it's running:**
```bash
mongo --eval "db.version()"
# or with mongosh
mongosh --eval "db.version()"
```

### 2. Configuration

MongoDB connection settings are in `appsettings.json`:

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "backgammon"
  }
}
```

**Production**: Update connection string with MongoDB Atlas or your hosted instance.

### 3. Run the Server

```bash
cd Backgammon.Web
dotnet run
```

The server automatically:
- Connects to MongoDB on startup
- Creates indexes for efficient queries
- Saves completed games when they end

## API Endpoints

### Game History
```bash
# Get player's game history
GET /api/player/{playerId}/games?limit=20&skip=0

# Get player statistics
GET /api/player/{playerId}/stats

# Get specific game details
GET /api/game/{gameId}

# Get database statistics
GET /api/stats/db
```

### Example Responses

**Player Stats:**
```json
{
  "playerId": "alice",
  "totalGames": 45,
  "wins": 28,
  "losses": 17,
  "totalStakes": 52,
  "normalWins": 20,
  "gammonWins": 6,
  "backgammonWins": 2,
  "winRate": 0.622
}
```

**Game Detail:**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "gameId": "abc123",
  "whitePlayerId": "alice",
  "redPlayerId": "bob",
  "moves": ["24/20", "13/9", "bar/24", "6/off"],
  "winner": "White",
  "stakes": 2,
  "moveCount": 87,
  "durationSeconds": 342,
  "completedAt": "2025-12-26T10:30:00Z"
}
```

## Swapping Database Providers

To switch from MongoDB to another database:

### 1. Create new implementation

```csharp
// Services/DynamoGameRepository.cs
public class DynamoGameRepository : IGameRepository
{
    public async Task SaveCompletedGameAsync(CompletedGame game)
    {
        // DynamoDB logic here
    }
    // ... implement other methods
}
```

### 2. Update DI registration

In [Program.cs](Backgammon.Web/Program.cs):

```csharp
// Before:
builder.Services.AddSingleton<IGameRepository, MongoGameRepository>();

// After:
builder.Services.AddSingleton<IGameRepository, DynamoGameRepository>();
```

**That's it!** No changes needed anywhere else.

## Data Model

### CompletedGame Document

```csharp
{
    _id: ObjectId("..."),              // MongoDB auto-generated ID
    gameId: "abc123",                  // Game session ID
    whitePlayerId: "alice",
    redPlayerId: "bob",
    moves: ["24/20", "13/9", ...],     // Move notation array
    diceRolls: ["3,4", "6,6", ...],    // Dice per turn (future)
    winner: "White",                   // "White" or "Red"
    stakes: 2,                         // 1=normal, 2=gammon, 3=backgammon
    turnCount: 42,                     // Total turns (future)
    moveCount: 87,                     // Total moves executed
    createdAt: ISODate("..."),         // Game start time
    completedAt: ISODate("..."),       // Game end time
    durationSeconds: 342               // Game duration
}
```

### Indexes

The repository automatically creates these indexes:
- `gameId` (unique) - Fast game lookups
- `whitePlayerId + completedAt` - Player game history
- `redPlayerId + completedAt` - Player game history
- `completedAt` - Recent games query

## Database Commands

```bash
# Connect to MongoDB shell
mongosh

# Switch to backgammon database
use backgammon

# View all games
db.games.find()

# Count total games
db.games.countDocuments()

# Find player's games
db.games.find({ $or: [
  { whitePlayerId: "alice" },
  { redPlayerId: "alice" }
]})

# Get stats by player
db.games.aggregate([
  { $match: { $or: [
    { whitePlayerId: "alice" },
    { redPlayerId: "alice" }
  ]}},
  { $group: {
    _id: null,
    totalGames: { $sum: 1 },
    totalStakes: { $sum: "$stakes" }
  }}
])
```

## Troubleshooting

**Connection Error:**
```
MongoDB connection failed
```
- Verify MongoDB is running: `brew services list` or `docker ps`
- Check connection string in `appsettings.json`

**Index Creation Warning:**
```
Failed to create indexes (may already exist)
```
- This is normal on restarts - indexes persist in MongoDB

**Games not saving:**
- Check application logs for errors
- Verify game actually completed (winner exists)
- Check MongoDB has write permissions

## Future Enhancements

- [ ] Track dice rolls per turn (`diceRolls` array)
- [ ] Track turn count
- [ ] Add game replay functionality
- [ ] Implement move analysis/patterns
- [ ] Add leaderboards
- [ ] Export games in standard notation
