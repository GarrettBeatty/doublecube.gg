---
sidebar_position: 2
---

# Match Play

DoubleCube.gg supports multi-game matches with proper backgammon scoring.

## Match Configuration

When creating a match, specify:

```typescript
const config = {
  targetScore: 7,        // First to 7 points wins
  opponentType: 'human', // 'human', 'ai', or 'friend'
  lobbyType: 'open',     // 'open', 'private', or 'friend'
  botId: null,           // AI bot ID if opponentType is 'ai'
  friendUserId: null     // Friend ID if lobbyType is 'friend'
};

await connection.invoke('CreateMatch', config);
```

## Scoring System

Points are awarded based on:
- **Normal win**: 1 point × cube value
- **Gammon**: 2 points × cube value
- **Backgammon**: 3 points × cube value

### Game Types

| Type | Condition |
|------|-----------|
| Normal | Opponent has borne off at least 1 checker |
| Gammon | Opponent has borne off 0 checkers |
| Backgammon | Gammon + opponent has checker on bar or in winner's home |

## Crawford Rule

The Crawford rule is automatically enforced:

1. When a player reaches `targetScore - 1`, the next game is the Crawford game
2. **No doubling is allowed** during the Crawford game
3. After the Crawford game, doubling resumes normally

```csharp
// In Backgammon.Core/Match.cs
public bool IsCrawfordGame =>
    !HasCrawfordGameBeenPlayed &&
    (Player1Score == TargetScore - 1 || Player2Score == TargetScore - 1);
```

## Match Flow

### Creation

1. Player creates match with configuration
2. Server creates match record in DynamoDB
3. First game session created automatically
4. `MatchCreated` event sent with match and game IDs

### Game Completion

1. Game ends (winner determined)
2. `MatchService.CompleteGameAsync()` updates scores
3. If match not complete, `MatchContinued` event sent
4. Players join next game automatically

### Match Completion

1. Final game completes
2. ELO ratings updated
3. Match marked complete in database
4. `MatchCompleted` event with final results

## Events

### MatchCreated

```typescript
connection.on('MatchCreated', (data) => {
  console.log('Match ID:', data.matchId);
  console.log('First game:', data.gameId);
});
```

### MatchUpdate

```typescript
connection.on('MatchUpdate', (data) => {
  console.log('Score:', data.player1Score, '-', data.player2Score);
  console.log('Crawford game:', data.isCrawfordGame);
});
```

### MatchCompleted

```typescript
connection.on('MatchCompleted', (data) => {
  console.log('Winner:', data.winnerId);
  console.log('Final score:', data.winnerScore, '-', data.loserScore);
  console.log('Rating change:', data.ratingChange);
});
```

## Continuing a Match

After a game ends but match continues:

```typescript
await connection.invoke('ContinueMatch', matchId);
```

This creates the next game and automatically joins both players.

## Match History

Get a player's match history:

```typescript
const matches = await connection.invoke('GetMyMatches', 'Completed');
```

Get games within a specific match:

```typescript
const games = await connection.invoke('GetMatchGames', matchId);
```
