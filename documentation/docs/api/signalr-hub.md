---
sidebar_position: 3
---

# SignalR Hub Reference

Real-time WebSocket methods for gameplay and communication.

## Connection

### Connecting

```typescript
import { HubConnectionBuilder } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl('http://localhost:5000/gamehub?access_token=' + token)
  .withAutomaticReconnect()
  .build();

await connection.start();
```

## Game Actions

### JoinGame

Join an existing game or create a new one.

```typescript
await connection.invoke('JoinGame', gameId);
// gameId: string | null - Pass null to create new game
```

### RollDice

Roll dice to start your turn.

```typescript
await connection.invoke('RollDice');
```

### MakeMove

Execute a checker move.

```typescript
await connection.invoke('MakeMove', from, to);
// from: number (1-24, 0 for bar)
// to: number (1-24, 25 for bear off)
```

### EndTurn

End your turn and pass to opponent.

```typescript
await connection.invoke('EndTurn');
```

### UndoLastMove

Undo the last move (current turn only).

```typescript
await connection.invoke('UndoLastMove');
```

### LeaveGame

Leave the current game.

```typescript
await connection.invoke('LeaveGame');
```

### AbandonGame

Forfeit the current game.

```typescript
await connection.invoke('AbandonGame');
```

## Doubling Cube

### OfferDouble

Offer to double the stakes.

```typescript
await connection.invoke('OfferDouble');
```

### AcceptDouble

Accept a double offer.

```typescript
await connection.invoke('AcceptDouble');
```

### DeclineDouble

Decline a double offer (opponent wins).

```typescript
await connection.invoke('DeclineDouble');
```

## Move Validation

### GetValidSources

Get points with moveable checkers.

```typescript
const sources: number[] = await connection.invoke('GetValidSources');
```

### GetValidDestinations

Get valid destinations from a source point.

```typescript
const moves: MoveDto[] = await connection.invoke('GetValidDestinations', fromPoint);
```

## Match Operations

### CreateMatch

Create a new match.

```typescript
await connection.invoke('CreateMatch', {
  targetScore: 5,
  opponentType: 'human', // 'human' | 'ai' | 'friend'
  lobbyType: 'open',     // 'open' | 'private' | 'friend'
  botId: null,
  friendUserId: null
});
```

### JoinMatch

Join an existing match.

```typescript
await connection.invoke('JoinMatch', matchId);
```

### ContinueMatch

Continue to next game in a match.

```typescript
await connection.invoke('ContinueMatch', matchId);
```

### GetMatchState

Get current match state.

```typescript
const state: MatchStateDto = await connection.invoke('GetMatchState', matchId);
```

### GetMyMatches

Get player's matches.

```typescript
const matches = await connection.invoke('GetMyMatches', 'InProgress');
// status: 'InProgress' | 'Completed' | null
```

## Analysis

### AnalyzePosition

Get position evaluation.

```typescript
const eval: PositionEvaluationDto = await connection.invoke(
  'AnalyzePosition',
  gameId,
  'heuristic' // 'heuristic' | 'gnubg'
);
```

### FindBestMoves

Get ranked move suggestions.

```typescript
const analysis: BestMovesAnalysisDto = await connection.invoke(
  'FindBestMoves',
  gameId,
  'gnubg'
);
```

### GetGameHistory

Get turn-by-turn replay data.

```typescript
const history: GameHistoryDto = await connection.invoke('GetGameHistory', gameId);
```

## Social

### GetFriends

Get friends list.

```typescript
const friends: FriendDto[] = await connection.invoke('GetFriends');
```

### SendChatMessage

Send chat message in current game.

```typescript
await connection.invoke('SendChatMessage', message);
```

## Server Events

### GameUpdate

Board state changed.

```typescript
connection.on('GameUpdate', (state: GameState) => {
  // Update UI with new state
});
```

### GameStart

Game begins (both players joined).

```typescript
connection.on('GameStart', (state: GameState) => {
  // Initialize game UI
});
```

### GameOver

Game completed.

```typescript
connection.on('GameOver', (state: GameState) => {
  // Show result
});
```

### DoubleOffered

Opponent offered double.

```typescript
connection.on('DoubleOffered', (offer: DoubleOfferDto) => {
  // Show accept/decline dialog
});
```

### MatchCompleted

Match finished.

```typescript
connection.on('MatchCompleted', (data: MatchCompletedDto) => {
  // Show match results, rating change
});
```

### Error

Server error occurred.

```typescript
connection.on('Error', (message: string) => {
  // Show error message
});
```

### ReceiveChatMessage

Chat message received.

```typescript
connection.on('ReceiveChatMessage', (sender, message, senderId) => {
  // Display chat message
});
```
