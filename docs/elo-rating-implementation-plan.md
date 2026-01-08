# ELO Rating System Implementation Plan

## Overview
Implement an ELO-based rating system to track player skill levels and enable skill-based matchmaking for the backgammon platform.

## Goals
- Calculate and update player ratings after each competitive game
- Display rating on player profiles and leaderboards
- Enable skill-based matchmaking
- Track rating history over time
- Support different rating pools (casual vs. ranked)

## ELO System Design

### Formula
```
NewRating = OldRating + K * (ActualScore - ExpectedScore)
```

Where:
- **ExpectedScore** = 1 / (1 + 10^((OpponentRating - PlayerRating) / 400))
- **ActualScore** = 1 for win, 0 for loss
- **K-factor**:
  - 32 for new players (< 30 games)
  - 24 for established players (≥ 30 games)

### Initial Rating
- New players start at **1500**
- This is the industry standard starting point for ELO systems

### Rating Pools
- **Casual**: For unranked games (not tracked for rating)
- **Ranked**: For competitive games that affect ratings

## Technical Implementation

### 1. Data Model Changes

#### User Model (`Backgammon.Server/Models/User.cs`)
Add rating-related properties to the `User` class:

```csharp
/// <summary>
/// Current ELO rating (1500 = starting rating)
/// </summary>
[JsonPropertyName("rating")]
public int Rating { get; set; } = 1500;

/// <summary>
/// Highest rating ever achieved
/// </summary>
[JsonPropertyName("peakRating")]
public int PeakRating { get; set; } = 1500;

/// <summary>
/// When the rating was last updated
/// </summary>
[JsonPropertyName("ratingLastUpdatedAt")]
public DateTime? RatingLastUpdatedAt { get; set; }

/// <summary>
/// Number of rated games played (used for K-factor calculation)
/// </summary>
[JsonPropertyName("ratedGamesCount")]
public int RatedGamesCount { get; set; } = 0;
```

#### Game Model (`Backgammon.Server/Models/Game.cs`)
Add fields to track if a game is rated:

```csharp
/// <summary>
/// Whether this game affects player ratings (ranked vs casual)
/// </summary>
[JsonPropertyName("isRanked")]
public bool IsRanked { get; set; } = true;

/// <summary>
/// White player's rating before the game
/// </summary>
[JsonPropertyName("whiteRatingBefore")]
public int? WhiteRatingBefore { get; set; }

/// <summary>
/// Red player's rating before the game
/// </summary>
[JsonPropertyName("redRatingBefore")]
public int? RedRatingBefore { get; set; }

/// <summary>
/// White player's rating after the game
/// </summary>
[JsonPropertyName("whiteRatingAfter")]
public int? WhiteRatingAfter { get; set; }

/// <summary>
/// Red player's rating after the game
/// </summary>
[JsonPropertyName("redRatingAfter")]
public int? RedRatingAfter { get; set; }
```

### 2. Rating Calculation Service

Create `Backgammon.Server/Services/EloRatingService.cs`:

```csharp
public interface IEloRatingService
{
    /// <summary>
    /// Calculate new ratings for both players after a game
    /// </summary>
    (int whiteNewRating, int redNewRating) CalculateNewRatings(
        int whiteRating,
        int redRating,
        int whiteRatedGames,
        int redRatedGames,
        bool whiteWon);

    /// <summary>
    /// Calculate expected score for a player
    /// </summary>
    double CalculateExpectedScore(int playerRating, int opponentRating);

    /// <summary>
    /// Get K-factor based on games played
    /// </summary>
    int GetKFactor(int gamesPlayed);
}

public class EloRatingService : IEloRatingService
{
    private const int StartingRating = 1500;
    private const int KFactorNew = 32;      // < 30 games
    private const int KFactorEstablished = 24; // >= 30 games
    private const int GamesForEstablished = 30;

    public (int whiteNewRating, int redNewRating) CalculateNewRatings(
        int whiteRating,
        int redRating,
        int whiteRatedGames,
        int redRatedGames,
        bool whiteWon)
    {
        var whiteExpected = CalculateExpectedScore(whiteRating, redRating);
        var redExpected = CalculateExpectedScore(redRating, whiteRating);

        var whiteActual = whiteWon ? 1.0 : 0.0;
        var redActual = whiteWon ? 0.0 : 1.0;

        var whiteK = GetKFactor(whiteRatedGames);
        var redK = GetKFactor(redRatedGames);

        var whiteChange = (int)Math.Round(whiteK * (whiteActual - whiteExpected));
        var redChange = (int)Math.Round(redK * (redActual - redExpected));

        return (whiteRating + whiteChange, redRating + redChange);
    }

    public double CalculateExpectedScore(int playerRating, int opponentRating)
    {
        return 1.0 / (1.0 + Math.Pow(10.0, (opponentRating - playerRating) / 400.0));
    }

    public int GetKFactor(int gamesPlayed)
    {
        return gamesPlayed < GamesForEstablished ? KFactorNew : KFactorEstablished;
    }
}
```

### 3. DynamoDB Schema Updates

#### User Repository Updates
Update `DynamoDbUserRepository` to handle rating fields:

- `UpdateRatingAsync(userId, rating, peakRating, ratingLastUpdatedAt, ratedGamesCount)` - Atomic update method
- Ensure `MarshalUser` and `UnmarshalUser` in `DynamoDbHelpers` include rating fields

#### Rating Leaderboard GSI (Optional - Phase 2)
Could add a new GSI (GSI4) for efficient leaderboard queries:
- **GSI4PK**: `"LEADERBOARD#RATING"`
- **GSI4SK**: `"{rating}#{userId}"` (padded for sorting)

For MVP, can use scan/filter on existing user records.

### 4. Game Completion Integration

Update `GameHub.cs` in the `UpdateUserStatsAfterGame` method:

```csharp
private async Task UpdateUserStatsAfterGame(Game game)
{
    // Existing validation...

    // Skip rating updates for AI games or unranked games
    if (game.IsAiOpponent || !game.IsRanked)
    {
        _logger.LogInformation("Skipping rating update for game {GameId} - AI or unranked", game.GameId);
        // Still update stats, just not ratings
        // ... existing stats code ...
        return;
    }

    // Both players must be registered for rating updates
    if (string.IsNullOrEmpty(game.WhiteUserId) || string.IsNullOrEmpty(game.RedUserId))
    {
        _logger.LogInformation("Skipping rating update for game {GameId} - anonymous player", game.GameId);
        return;
    }

    // Get both users
    var whiteUser = await _userRepository.GetByUserIdAsync(game.WhiteUserId);
    var redUser = await _userRepository.GetByUserIdAsync(game.RedUserId);

    if (whiteUser == null || redUser == null)
    {
        _logger.LogWarning("Failed to get users for rating update in game {GameId}", game.GameId);
        return;
    }

    // Calculate new ratings
    var whiteWon = game.Winner == "White";
    var (whiteNewRating, redNewRating) = _eloRatingService.CalculateNewRatings(
        whiteUser.Rating,
        redUser.Rating,
        whiteUser.RatedGamesCount,
        redUser.RatedGamesCount,
        whiteWon
    );

    // Update white player
    whiteUser.Rating = whiteNewRating;
    whiteUser.PeakRating = Math.Max(whiteUser.PeakRating, whiteNewRating);
    whiteUser.RatingLastUpdatedAt = DateTime.UtcNow;
    whiteUser.RatedGamesCount++;
    UpdateStats(whiteUser.Stats, whiteWon, game.Stakes);
    await _userRepository.UpdateUserAsync(whiteUser);

    // Update red player
    redUser.Rating = redNewRating;
    redUser.PeakRating = Math.Max(redUser.PeakRating, redNewRating);
    redUser.RatingLastUpdatedAt = DateTime.UtcNow;
    redUser.RatedGamesCount++;
    UpdateStats(redUser.Stats, !whiteWon, game.Stakes);
    await _userRepository.UpdateUserAsync(redUser);

    // Save rating changes to game record
    game.WhiteRatingBefore = whiteUser.Rating - (whiteNewRating - whiteUser.Rating);
    game.RedRatingBefore = redUser.Rating - (redNewRating - redUser.Rating);
    game.WhiteRatingAfter = whiteNewRating;
    game.RedRatingAfter = redNewRating;
    await _gameRepository.UpdateGameAsync(game);

    _logger.LogInformation("Updated ratings for game {GameId}: White {WhiteBefore}→{WhiteAfter}, Red {RedBefore}→{RedAfter}",
        game.GameId, game.WhiteRatingBefore, game.WhiteRatingAfter, game.RedRatingBefore, game.RedRatingAfter);
}
```

### 5. API Endpoints

Add endpoints to `GameHub.cs` or create a separate `LeaderboardHub.cs`:

```csharp
/// <summary>
/// Get top players by rating
/// </summary>
public async Task<List<LeaderboardEntryDto>> GetLeaderboard(int limit = 50)
{
    // Implementation would scan users or use GSI4
}

/// <summary>
/// Get player's rating rank
/// </summary>
public async Task<int?> GetPlayerRank(string userId)
{
    // Implementation to find player's rank
}

/// <summary>
/// Get rating history for a player (optional - Phase 2)
/// </summary>
public async Task<List<RatingHistoryDto>> GetRatingHistory(string userId, int limit = 50)
{
    // Would query rating history records
}
```

### 6. Matchmaking Integration

Update matchmaking logic to use ratings:

```csharp
// In GameSessionManager or new MatchmakingService
public GameSession? FindMatchByRating(string playerId, int playerRating, int ratingRange = 200)
{
    var waitingGames = _sessions.Values
        .Where(s => !s.IsFull && s.IsRanked)
        .ToList();

    foreach (var game in waitingGames)
    {
        var opponentId = game.WhitePlayerId ?? game.RedPlayerId;
        if (opponentId == null) continue;

        var opponent = await _userRepository.GetByUserIdAsync(opponentId);
        if (opponent == null) continue;

        var ratingDiff = Math.Abs(playerRating - opponent.Rating);
        if (ratingDiff <= ratingRange)
        {
            return game;
        }
    }

    return null; // No suitable match found
}
```

## Implementation Phases

### Phase 1: Core Rating System (This PR)
1. Add rating fields to User and Game models
2. Implement EloRatingService
3. Update DynamoDB helpers to marshal/unmarshal rating fields
4. Update GameHub to calculate and save ratings after games
5. Add basic tests for ELO calculation
6. Update existing users to have default rating (1500)

### Phase 2: Leaderboards & Display (Follow-up)
1. Create leaderboard query endpoints
2. Add rating to profile display
3. Show rating changes after game completion
4. Add rating history tracking (separate DynamoDB items)

### Phase 3: Matchmaking Integration (Follow-up)
1. Add ranked/casual game mode selection
2. Implement rating-based matchmaking
3. Display opponent rating in game lobby
4. Add rating-based game filtering

### Phase 4: Advanced Features (Future)
1. Rating history graphs
2. Rating decay for inactive players
3. Seasonal rating resets
4. Separate rating pools for different game variants
5. Provisional ratings for new players

## Testing Strategy

### Unit Tests
- `EloRatingService` calculation accuracy
- K-factor selection logic
- Edge cases (rating boundaries, equal ratings, large rating differences)

### Integration Tests
- End-to-end game completion with rating updates
- Verify both players' ratings are updated atomically
- Verify AI games don't affect ratings
- Verify anonymous games don't affect ratings

### Manual Testing
- Play ranked games and verify rating changes
- Check profile displays correct rating
- Verify leaderboard ordering
- Test matchmaking with different rating levels

## Database Migration

For existing users without ratings:
```csharp
// Run once to set default ratings for all existing users
public async Task MigrateExistingUsers()
{
    // Scan all users
    // Set Rating = 1500, PeakRating = 1500, RatedGamesCount = 0
    // Update each user
}
```

## Configuration

Add to `appsettings.json`:
```json
{
  "EloRating": {
    "StartingRating": 1500,
    "KFactorNew": 32,
    "KFactorEstablished": 24,
    "GamesForEstablished": 30,
    "DefaultMatchmakingRange": 200
  }
}
```

## Dependencies

No new external dependencies required. Uses existing:
- DynamoDB for storage
- SignalR for real-time updates
- Existing User and Game models

## Risks & Mitigations

### Risk: Rating inflation/deflation
**Mitigation**: Monitor average rating over time, adjust K-factors if needed

### Risk: Rating manipulation
**Mitigation**: Flag suspicious patterns (same players repeatedly, win trading)

### Risk: Performance on leaderboard queries
**Mitigation**: Use caching, pagination, GSI for efficient queries

### Risk: Retroactive rating calculation
**Mitigation**: Only rate games going forward, or batch-process historical games

### Risk: Race condition in concurrent rating updates
**Issue**: If a player completes two games simultaneously, rating updates could be lost due to read-modify-write pattern without locking.

**Example**: Player completes Game 1 and Game 2 concurrently:
- Both transactions read current rating (1500)
- Game 1 updates to 1516
- Game 2 updates to 1484 (based on stale 1500 read)
- Final rating is 1484, losing the +16 from Game 1

**Potential Mitigations** (to be implemented before production with concurrent users):
1. **DynamoDB Conditional Writes** (Best for AWS deployment)
   - Add version number to User model
   - Use conditional expressions: `SET rating = :newRating WHERE version = :expectedVersion`
   - Retry on condition failure with fresh read
2. **Application-level distributed locking**
   - Use Redis or DynamoDB Lock Client
   - Serialize rating updates per userId
3. **Queue-based processing**
   - Queue rating updates per player
   - Process serially with single-threaded consumer per player

**Current Status**: TODO comment exists in `PlayerStatsService.cs`. Must be addressed before production deployment.

## Success Metrics

- Ratings accurately reflect player skill
- Matchmaking produces balanced games (50% win rate at similar ratings)
- Players engage with ranked mode
- Leaderboard queries perform well (<500ms)

## Implementation Decisions

### 1. Minimum Rating Floor
**Decision**: ✅ Implemented with minimum rating of 100 (defined in `User.MinimumRating`).

**Rationale**: Prevents ratings from going negative and provides a stable floor for new/struggling players.

### 2. Stakes Incorporation in ELO Calculation
**Decision**: ✅ Implemented - Stakes (WinType × DoublingCube value) are incorporated as a multiplier in rating changes.

**Formula**: `RatingChange = K × (ActualScore - ExpectedScore) × Stakes`

**Rationale**:
- Aligns with competitive backgammon rating systems (FIBS)
- Rewards skill in achieving gammon/backgammon wins appropriately
- Reflects that winning with higher stakes demonstrates greater skill
- Stakes values: Normal=1, Gammon=2, Backgammon=3, multiplied by cube value

**Implementation**: See `EloRatingService.CalculateNewRatings()` - stakes parameter added.

### 3. AI Games and Rating
**Decision**: ✅ AI games are always unrated, regardless of lobby settings.

**Rationale**: Prevents rating inflation/manipulation. AI difficulty is not calibrated to player ratings.

**Enforcement**:
- Backend: `GameEngineMapper.ToGame()` forces `IsRated = false` when `IsAiOpponent = true`
- Frontend: `CreateMatchModal.tsx` disables rated toggle for AI opponents

## Open Questions

1. ~~Should we allow rating to go below a minimum (e.g., 100)?~~ ✅ **Resolved**: Minimum of 100 implemented
2. Should we implement rating decay for inactive players? (Phase 4)
3. How to handle disconnections/abandonments in ranked games?
4. ~~Should doubles/gammon/backgammon affect rating differently?~~ ✅ **Resolved**: Stakes incorporated as multiplier
5. Display rating publicly or only to the player?

## References

- [Elo Rating System - Wikipedia](https://en.wikipedia.org/wiki/Elo_rating_system)
- Similar implementations in chess, gaming platforms
- Industry standard K-factors and rating ranges
