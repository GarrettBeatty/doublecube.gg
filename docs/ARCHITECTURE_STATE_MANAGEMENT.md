# Architecture: State Management & Persistence

This document describes the current state management architecture, known issues, and planned improvements.

## Overview

The codebase manages state across three layers that can diverge:

1. **In-memory** - `GameSession` / `GameEngine` (during gameplay)
2. **DynamoDB** - `Match` / `Game` records (persistence)
3. **Core domain** - `Core.Match` / `Core.Game` (business logic)

---

## Current Architecture

### Game State Flow

```
Player makes move
    │
    ▼
GameSession (in-memory)
    │ GameEngine validates & executes
    ▼
Broadcast to clients (SignalR)
    │
    ▼
Save to DynamoDB (fire-and-forget) ← PROBLEM: can fail silently
```

### Match State Flow

```
Match created
    │
    ▼
Save to DynamoDB
    │
    ▼
Game added
    │ AddGameToMatchAsync (atomic list_append)
    │ OR UpdateMatchAsync (full overwrite) ← PROBLEM: dual paths
    ▼
Game completes
    │ CoreMatch.AddGame() (in-memory)
    │ UpdateMatchAsync (persist)
    ▼
Match updated in DB
```

---

## Known Issues

### P0: Critical (Data Loss Risk)

#### 1. Fire-and-Forget Persistence

**Location**: `GameService.cs`, `GameActionOrchestrator.cs`

```csharp
BackgroundTaskHelper.FireAndForget(async () => {
    await _gameRepository.SaveGameAsync(game);
}, _logger, ...);
```

**Problem**: Save failures are logged but game continues. Server crash = data loss.

**Fix**: Await all saves, handle failures.

#### 2. Broadcast-Then-Save Race

**Location**: `GameActionOrchestrator.cs`

```csharp
await _broadcastService.BroadcastGameUpdateAsync(session);  // Client sees state
await SaveGameStateAsync(session);  // Can fail after!
```

**Problem**: Client thinks move succeeded, but DB might not have it.

**Fix**: Save first, then broadcast.

### P1: High (Data Inconsistency)

#### 3. Dual-Path Updates

**Problem**: Same DB field updated via multiple code paths.

| Field | Path 1 | Path 2 |
|-------|--------|--------|
| `gameIds` | `AddGameToMatchAsync` (list_append) | `UpdateMatchAsync` (overwrite) |
| `currentGameId` | `AddGameToMatchAsync` | `UpdateMatchAsync` |

**Fix**: Single owner per field, document which method owns what.

#### 4. Hollow Computed Properties

**Location**: `Match.cs`

```csharp
public List<string> GameIds
{
    get => CoreMatch.Games.Select(g => g.GameId).ToList();
    set => CoreMatch.Games = value.Select(id => new Core.Game(id)).ToList(); // Hollow!
}
```

**Problem**: After loading from DB, `CoreMatch.Games` contains Game objects with only IDs - no winner, stakes, or move history.

**Fix**: Hybrid approach - store game summaries in Match, only hydrate full games when needed.

### P2: Medium

#### 5. Reflection-Based State Restoration

**Location**: `GameEngineMapper.FromGame()`

Uses reflection to set readonly backing fields. Fragile - can break with compiler changes.

#### 6. Missing MoveHistory Reconstruction

Move history is serialized as notation strings but not reconstructed on server restart.

---

## Planned Solution: Hybrid Game Storage

### Approach

Store **game summaries** embedded in Match, keep **full games** in Game table.

### Match Document (New)

```json
{
  "matchId": "match-123",
  "player1Score": 3,
  "player2Score": 5,
  "status": "InProgress",
  "gamesSummary": [
    {
      "gameId": "game-1",
      "winner": "White",
      "stakes": 2,
      "winType": "Gammon",
      "isCrawford": false
    }
  ],
  "currentGameId": "game-2"
}
```

### Benefits

1. **No hollow objects** - Summaries have real data
2. **Single read** - Match page gets everything
3. **No 400KB limit issue** - Summaries are small
4. **Full game available** - Load from Game table for replay

### CoreMatch Hydration

- `GamesSummary` for persistence/display (always available)
- `CoreMatch.Games` only populated on-demand for domain logic

---

## Property Changes

| Property | Before | After |
|----------|--------|-------|
| `GameIds` | Computed from `CoreMatch.Games` (hollow) | Computed from `GamesSummary` |
| `TotalGamesPlayed` | `CoreMatch.Games.Count` | `GamesSummary.Count` |
| `CurrentGameId` | Creates hollow `Core.Game` | Simple string storage |

---

## Source of Truth

| Entity | During Gameplay | After Completion | Authoritative |
|--------|----------------|------------------|---------------|
| Game state | In-memory `GameSession` | DynamoDB | Memory → DB on save |
| Match state | DynamoDB | DynamoDB | DB (fetched per request) |
| User state | DynamoDB | DynamoDB | DB |

---

## Implementation Priority

### P0: Stop Data Loss
1. Await all game saves (remove FireAndForget)
2. Save before broadcast
3. Add retry logic

### P1: Fix Inconsistencies
4. Implement hybrid game storage (`GamesSummary`)
5. Single path per DB field
6. Remove hollow computed properties

### P2: Improve Reliability
7. Optimistic locking (version field)
8. DynamoDB transactions for multi-item updates
9. Reconstruct MoveHistory on load

---

## Files Requiring Changes

| File | Issues | Priority |
|------|--------|----------|
| `GameActionOrchestrator.cs` | Fire-and-forget, broadcast-then-save | P0 |
| `GameService.cs` | Fire-and-forget saves | P0 |
| `Match.cs` | Computed properties, GamesSummary | P1 |
| `MatchService.cs` | Non-atomic operations | P1 |
| `DynamoDbMatchRepository.cs` | Field ownership | P1 |
| `DynamoDbHelpers.cs` | Marshal/unmarshal GamesSummary | P1 |
| `GameEngineMapper.cs` | Reflection, MoveHistory | P2 |
