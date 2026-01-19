---
sidebar_position: 4
---

# Position Analysis

DoubleCube.gg provides position evaluation and move analysis features.

## Evaluators

### Heuristic Evaluator

A built-in fast evaluator using traditional backgammon heuristics:

- Pip count advantage
- Home board strength
- Blot exposure
- Anchor positions
- Bearing off race

### GNU Backgammon Evaluator

Integration with GNU Backgammon for expert-level analysis:

- World-class neural network evaluation
- Rollout capability
- Cube decision analysis

See [GNU Backgammon Setup](/deployment/gnubg-setup) for configuration.

## Analysis Features

### Position Evaluation

Get win/gammon/backgammon probabilities:

```typescript
const evaluation = await connection.invoke('AnalyzePosition', gameId, 'heuristic');

console.log('Win probability:', evaluation.winProbability);
console.log('Gammon probability:', evaluation.gammonProbability);
console.log('Backgammon probability:', evaluation.backgammonProbability);
```

### Best Move Analysis

Find the best moves for a position:

```typescript
const analysis = await connection.invoke('FindBestMoves', gameId, 'gnubg');

analysis.rankedMoves.forEach((move, i) => {
  console.log(`${i+1}. ${move.notation} - Equity: ${move.equity}`);
});
```

## Analysis Mode

The analysis board allows free position manipulation:

```typescript
// Create an analysis session
const sessionId = await connection.invoke('CreateAnalysisSession');

// Set custom dice
await connection.invoke('SetDice', 3, 1);

// Move checkers freely
await connection.invoke('MoveCheckerDirectly', 24, 21);

// Change whose turn
await connection.invoke('SetCurrentPlayer', 'White');
```

### Import/Export Positions

```typescript
// Export current position
const sgf = await connection.invoke('ExportPosition');

// Import a position
await connection.invoke('ImportPosition', sgfData);

// Export full game with move history
const gameSgf = await connection.invoke('ExportGameSgf');
```

## Game History Replay

Replay completed games move by move:

```typescript
const history = await connection.invoke('GetGameHistory', gameId);

history.turns.forEach((turn) => {
  console.log(`Turn ${turn.turnNumber}:`);
  console.log(`  Dice: ${turn.die1}, ${turn.die2}`);
  turn.moves.forEach(m => console.log(`  Move: ${m.from} → ${m.to}`));
});
```

## Evaluator Selection

Different evaluators suit different needs:

| Evaluator | Speed | Accuracy | Use Case |
|-----------|-------|----------|----------|
| Heuristic | Fast | Good | Real-time hints |
| GNU Backgammon | Slow | Excellent | Post-game analysis |

## API Reference

### AnalyzePosition

```typescript
AnalyzePosition(gameId: string, evaluatorType?: string): Promise<PositionEvaluation>
```

### FindBestMoves

```typescript
FindBestMoves(gameId: string, evaluatorType?: string): Promise<BestMovesAnalysis>
```

### GetGameHistory

```typescript
GetGameHistory(gameId: string): Promise<GameHistory | null>
```

### ParseGameSgf

```typescript
ParseGameSgf(sgf: string): Promise<GameHistory | null>
```
