---
sidebar_position: 5
---

# AI Opponents

DoubleCube.gg provides multiple AI opponents for single-player games.

## Available Bots

### Random AI

Makes completely random valid moves. Useful as a baseline.

- **Difficulty**: Beginner
- **Estimated ELO**: ~800

### Greedy AI

Prioritizes:
1. Bearing off checkers
2. Hitting opponent blots
3. Moving to safe positions

- **Difficulty**: Intermediate
- **Estimated ELO**: ~1100

### Heuristic AI

Uses position evaluation heuristics:
- Pip count optimization
- Home board building
- Anchor strategy
- Blot minimization

- **Difficulty**: Advanced
- **Estimated ELO**: ~1300

### GNU Backgammon AI

Uses GNU Backgammon's neural network for expert-level play.

- **Difficulty**: Expert
- **Estimated ELO**: ~1800+
- **Requires**: GNU Backgammon installation

## Playing Against AI

### Create AI Match

```typescript
const config = {
  targetScore: 5,
  opponentType: 'ai',
  botId: 'greedy'  // 'random', 'greedy', 'heuristic', 'gnubg'
};

await connection.invoke('CreateMatch', config);
```

### Get Available Bots

```typescript
const bots = await connection.invoke('GetAvailableBots');

bots.forEach(bot => {
  console.log(`${bot.botId}: ${bot.displayName}`);
  console.log(`  ELO: ~${bot.estimatedElo}`);
  console.log(`  ${bot.description}`);
});
```

## AI Architecture

### IBackgammonAI Interface

```csharp
public interface IBackgammonAI
{
    string Name { get; }
    void ChooseMoves(GameEngine engine);
}
```

### Implementation Example

```csharp
public class MyCustomAI : IBackgammonAI
{
    public string Name => "Custom AI";

    public void ChooseMoves(GameEngine engine)
    {
        while (engine.RemainingMoves.Count > 0)
        {
            var validMoves = engine.GetValidMoves();
            if (validMoves.Count == 0) break;

            var bestMove = EvaluateMoves(validMoves, engine);
            engine.ExecuteMove(bestMove);
        }
    }

    private Move EvaluateMoves(List<Move> moves, GameEngine engine)
    {
        // Custom evaluation logic
    }
}
```

## Bot Registration

Bots are registered via the plugin system:

```csharp
// In Startup
builder.Services.AddStandardBots();      // Random, Greedy
builder.Services.AddHeuristicBot();      // Heuristic evaluator
builder.Services.AddAnalysisPlugins();   // GNU Backgammon (if available)
```

## AI Move Service

The server handles AI moves asynchronously:

```csharp
public interface IAiMoveService
{
    Task ExecuteAiTurnAsync(GameSession session);
}
```

When playing against AI:
1. Human makes move, ends turn
2. Server triggers `AiMoveService.ExecuteAiTurnAsync()`
3. AI evaluates position, makes moves
4. `GameUpdate` broadcast to human player

## Creating Custom Bots

1. Implement `IBackgammonAI`
2. Register with the bot resolver
3. Add metadata for display

```csharp
public class BotMetadata
{
    public string BotId { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public int EstimatedElo { get; set; }
    public bool RequiresExternalResources { get; set; }
}
```

## Simulation Mode

Run AI vs AI simulations:

```bash
cd Backgammon.AI
dotnet run
```

Select matchups and number of games to analyze AI performance.
