---
sidebar_position: 3
---

# ELO Rating System

DoubleCube.gg uses an ELO-based rating system to track player skill.

## How It Works

### Base Formula

The ELO system uses the standard formula with adjustments for backgammon:

```
Expected Score = 1 / (1 + 10^((OpponentRating - MyRating) / 400))
New Rating = Old Rating + K × (Actual - Expected)
```

### K-Factor

The K-factor determines rating volatility:

| Games Played | K-Factor |
|--------------|----------|
| 0-10 | 40 |
| 11-30 | 32 |
| 30+ | 24 |

New players have higher K-factors for faster calibration.

### Match Adjustments

Ratings are calculated per **match**, not per game:

- Match winner gets full win credit
- Game stakes (cube value) affect point differential
- Gammon/backgammon wins count more heavily

## Rating Changes

### Example Calculation

Player A (1200) vs Player B (1400):

```
Expected A = 1 / (1 + 10^((1400-1200)/400)) = 0.24
Expected B = 1 / (1 + 10^((1200-1400)/400)) = 0.76

If A wins:
New A = 1200 + 32 × (1.0 - 0.24) = 1224
New B = 1400 + 32 × (0.0 - 0.76) = 1376
```

An upset win results in larger rating changes.

### Gammon Bonus

Winning by gammon or backgammon provides additional rating credit:

| Win Type | Rating Multiplier |
|----------|-------------------|
| Normal | 1.0× |
| Gammon | 1.2× |
| Backgammon | 1.5× |

## API Methods

### Get Rating History

```typescript
const history = await connection.invoke('GetRatingHistory', 50);
// Returns last 50 rating changes with dates
```

### Get Leaderboard

```typescript
const leaderboard = await connection.invoke('GetLeaderboard', 100);
// Returns top 100 players by rating
```

### Get Rating Distribution

```typescript
const distribution = await connection.invoke('GetRatingDistribution');
// Returns histogram of all player ratings
```

## Rating Tiers

| Tier | Rating Range |
|------|--------------|
| Beginner | < 1000 |
| Intermediate | 1000 - 1200 |
| Advanced | 1200 - 1400 |
| Expert | 1400 - 1600 |
| Master | 1600+ |

## Implementation Details

The `EloRatingService` handles all calculations:

```csharp
public interface IEloRatingService
{
    (int newWinnerRating, int newLoserRating) CalculateNewRatings(
        int winnerRating,
        int loserRating,
        bool isGammon);

    int GetKFactor(int gamesPlayed);
}
```

Ratings are updated atomically when a match completes:

```csharp
// In MatchService.CompleteMatchAsync()
var (newWinnerRating, newLoserRating) = _eloService.CalculateNewRatings(
    winnerRating,
    loserRating,
    isGammon);

await _userRepository.UpdateRatingAsync(winnerId, newWinnerRating);
await _userRepository.UpdateRatingAsync(loserId, newLoserRating);
```

## Starting Rating

New players start at **1200** rating, which represents an average player.
