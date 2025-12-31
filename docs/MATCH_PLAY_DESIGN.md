# Match Play Design Document

## Overview
This document outlines the technical design for implementing match play in the doublecube.gg backgammon application, including support for the Crawford rule and proper doubling cube integration. The design prioritizes backward compatibility, maintainability, and a seamless user experience for competitive match play.

## Table of Contents
1. [Core Architecture](#core-architecture)
2. [Crawford Rule Implementation](#crawford-rule-implementation)
3. [Database Schema](#database-schema)
4. [Multiplayer Support](#multiplayer-support)
5. [UI/UX Design](#uiux-design)
6. [Implementation Plan](#implementation-plan)
7. [Testing Strategy](#testing-strategy)
8. [Migration Path](#migration-path)

## Core Architecture

### MatchEngine Class
The `MatchEngine` will wrap multiple `GameEngine` instances, managing the transition between games and enforcing match-specific rules.

```csharp
namespace Backgammon.Core
{
    public class MatchEngine
    {
        public int TargetScore { get; }
        public int WhiteScore { get; private set; }
        public int RedScore { get; private set; }
        public bool IsCrawfordGame { get; private set; }
        public bool CrawfordGamePlayed { get; private set; }
        public GameEngine CurrentGame { get; private set; }
        public List<GameResult> GameHistory { get; }
        
        public MatchEngine(int targetScore)
        {
            TargetScore = targetScore;
            GameHistory = new List<GameResult>();
            StartNewGame();
        }
        
        public void StartNewGame()
        {
            // Check if this should be a Crawford game
            var atMatchPoint = (WhiteScore == TargetScore - 1) || 
                               (RedScore == TargetScore - 1);
            var onlyOneAtMatchPoint = (WhiteScore == TargetScore - 1) ^ 
                                      (RedScore == TargetScore - 1);
                                      
            if (atMatchPoint && onlyOneAtMatchPoint && !CrawfordGamePlayed)
            {
                IsCrawfordGame = true;
                CrawfordGamePlayed = true;
            }
            else
            {
                IsCrawfordGame = false;
            }
            
            CurrentGame = new GameEngine(new MatchContext 
            { 
                IsCrawfordGame = IsCrawfordGame,
                WhiteMatchScore = WhiteScore,
                RedMatchScore = RedScore,
                TargetScore = TargetScore
            });
        }
        
        public void CompleteCurrentGame()
        {
            var result = new GameResult
            {
                Winner = CurrentGame.Winner,
                Points = CalculateGamePoints(),
                WasDoubled = CurrentGame.DoublingCube.CurrentValue > 1
            };
            
            GameHistory.Add(result);
            
            if (result.Winner == CheckerColor.White)
                WhiteScore += result.Points;
            else
                RedScore += result.Points;
                
            if (!IsMatchComplete())
                StartNewGame();
        }
        
        public bool IsMatchComplete() => 
            WhiteScore >= TargetScore || RedScore >= TargetScore;
            
        public CheckerColor? GetMatchWinner() =>
            WhiteScore >= TargetScore ? CheckerColor.White :
            RedScore >= TargetScore ? CheckerColor.Red : null;
        
        private int CalculateGamePoints()
        {
            var basePoints = 1;
            if (CurrentGame.IsGammon()) basePoints = 2;
            if (CurrentGame.IsBackgammon()) basePoints = 3;
            
            return basePoints * CurrentGame.DoublingCube.CurrentValue;
        }
    }
}
```

### GameEngine Modifications
Minimal changes to `GameEngine` to support match context:

```csharp
public class GameEngine
{
    // Existing properties...
    
    public MatchContext? MatchContext { get; }
    
    public GameEngine(MatchContext? matchContext = null)
    {
        MatchContext = matchContext;
        // Existing initialization...
        
        // Disable doubling cube for Crawford games
        if (matchContext?.IsCrawfordGame == true)
        {
            DoublingCube.DisableDoubling();
        }
    }
}

public class MatchContext
{
    public bool IsCrawfordGame { get; set; }
    public int WhiteMatchScore { get; set; }
    public int RedMatchScore { get; set; }
    public int TargetScore { get; set; }
}
```

### DoublingCube Modifications
Add support for disabling the cube during Crawford games:

```csharp
public class DoublingCube
{
    // Existing properties...
    private bool _doublingDisabled;
    
    public bool CanDouble(CheckerColor player) =>
        !_doublingDisabled && 
        !HasBeenOffered &&
        (Owner == null || Owner == player);
        
    public void DisableDoubling()
    {
        _doublingDisabled = true;
    }
}
```

## Crawford Rule Implementation

The Crawford rule states that when one player reaches match point minus one, the doubling cube cannot be used in the next game. Key implementation details:

1. **Automatic Detection**: The system automatically detects when Crawford rule should apply
2. **Single Game Only**: Crawford rule only applies to one game, then normal doubling resumes
3. **Server Enforcement**: All Crawford rule logic is enforced server-side for security
4. **Clear UI Indication**: Players are notified when playing a Crawford game

## Database Schema

### Match Entity
Extends the existing DynamoDB single-table design:

```
Primary Key (PK): MATCH#{matchId}
Sort Key (SK): METADATA

Attributes:
{
  "matchId": "uuid",
  "createdAt": "ISO8601",
  "updatedAt": "ISO8601",
  "whitePlayerId": "string",
  "redPlayerId": "string",
  "targetScore": 21,
  "whiteScore": 0,
  "redScore": 0,
  "currentGameId": "uuid",
  "gameIds": ["uuid1", "uuid2"],
  "status": "WAITING|IN_PROGRESS|COMPLETED",
  "winner": "WHITE|RED|null",
  "crawfordGamePlayed": false,
  "settings": {
    "jacobyRule": false,
    "beaversAllowed": false
  }
}
```

### Player-Match Index
For querying matches by player:

```
PK: USER#{playerId}
SK: MATCH#{reversedTimestamp}#{matchId}
```

### Game Entity Updates
Add match reference to existing game entities:

```
{
  // Existing game attributes...
  "matchId": "uuid|null",
  "matchGameNumber": 1,
  "isCrawfordGame": false
}
```

### GSI Updates
Add new GSI for match queries:

```
GSI4:
  GSI4PK: MATCH_STATUS#{status}
  GSI4SK: {timestamp}
```

## Multiplayer Support

### MatchSession Class
Manages multiple game sessions within a match:

```csharp
public class MatchSession
{
    private readonly MatchEngine _matchEngine;
    private readonly IGameSessionManager _gameSessionManager;
    private readonly IMatchRepository _matchRepository;
    private GameSession _currentGameSession;
    
    public string MatchId { get; }
    public string WhitePlayerId { get; }
    public string RedPlayerId { get; }
    
    public async Task StartMatch()
    {
        _matchEngine.StartNewGame();
        _currentGameSession = await _gameSessionManager.CreateGameSession(
            WhitePlayerId, 
            RedPlayerId,
            _matchEngine.CurrentGame,
            MatchId
        );
        
        await NotifyMatchStarted();
    }
    
    public async Task HandleGameComplete()
    {
        _matchEngine.CompleteCurrentGame();
        await _matchRepository.UpdateMatch(MatchId, _matchEngine);
        
        if (_matchEngine.IsMatchComplete())
        {
            await NotifyMatchComplete();
        }
        else
        {
            _currentGameSession = await _gameSessionManager.CreateGameSession(
                WhitePlayerId,
                RedPlayerId,
                _matchEngine.CurrentGame,
                MatchId
            );
            await NotifyNewGameStarted();
        }
    }
}
```

### SignalR Events
New events for match play:

```csharp
public interface IGameClient
{
    // Existing events...
    
    // Match events
    Task MatchStarted(MatchStartedDto match);
    Task MatchScoreUpdated(MatchScoreDto score);
    Task CrawfordGameStarted();
    Task GameCompleteInMatch(GameResultDto result);
    Task MatchComplete(MatchResultDto result);
}
```

### GameHub Updates
Add match-aware methods:

```csharp
public class GameHub : Hub<IGameClient>
{
    // Existing methods...
    
    public async Task<MatchCreatedDto> CreateMatch(CreateMatchRequest request)
    {
        var match = await _matchService.CreateMatch(
            Context.UserIdentifier,
            request.OpponentId,
            request.TargetScore
        );
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{match.Id}");
        return new MatchCreatedDto(match);
    }
    
    public async Task JoinMatch(string matchId)
    {
        var match = await _matchService.GetMatch(matchId);
        ValidatePlayerInMatch(match, Context.UserIdentifier);
        
        await Groups.AddToGroupAsync(Context.ConnectionId, $"match-{matchId}");
        await Clients.Caller.MatchStarted(new MatchStartedDto(match));
    }
}
```

## UI/UX Design

### Match Score Display
Prominent display of match score in the game interface:

```
┌─────────────────────────────────────┐
│   Match to 21 (Crawford: No)       │
│   White: 15  ████████████░░░░░ 21  │
│   Red:   18  ██████████████░░ 21   │
└─────────────────────────────────────┘
```

### Game Transition Screen
Between games in a match:

```
┌─────────────────────────────────────┐
│         GAME COMPLETE               │
│                                     │
│   White wins 2 points!              │
│                                     │
│   Match Score:                      │
│   White: 15 → 17                    │
│   Red:   18                         │
│                                     │
│   [Continue to Next Game]           │
└─────────────────────────────────────┘
```

### Crawford Game Notification
Clear indication when playing a Crawford game:

```
┌─────────────────────────────────────┐
│    ⚠️ CRAWFORD GAME IN EFFECT ⚠️     │
│  Doubling cube disabled this game   │
└─────────────────────────────────────┘
```

### Match Setup Dialog
```
┌─────────────────────────────────────┐
│        CREATE NEW MATCH             │
├─────────────────────────────────────┤
│ Opponent: [Select Friend ▼]         │
│                                     │
│ Match Length:                       │
│ ○ 5 points  (Quick)                 │
│ ○ 11 points (Medium)                │
│ ● 21 points (Standard)              │
│ ○ Custom: [___] points              │
│                                     │
│ Advanced Options:                   │
│ ☐ Jacoby Rule                       │
│ ☐ Allow Beavers                     │
│                                     │
│ [Cancel]         [Create Match]     │
└─────────────────────────────────────┘
```

### Mobile Responsive Design
- Collapsible match score panel on small screens
- Swipe gestures to view match history
- Compact Crawford indicator

## Implementation Plan

### Phase 1: Core Match Logic (Week 1-2)
1. Implement `MatchEngine` class
2. Add `MatchContext` to `GameEngine`
3. Update `DoublingCube` for Crawford support
4. Unit tests for match logic

### Phase 2: Database Integration (Week 2-3)
1. Create match repository interfaces
2. Implement DynamoDB match persistence
3. Update game entities with match references
4. Integration tests for data layer

### Phase 3: Multiplayer Support (Week 3-4)
1. Implement `MatchSession` class
2. Add SignalR match events
3. Update `GameHub` with match methods
4. Test multiplayer match flow

### Phase 4: UI Implementation (Week 4-5)
1. Design match UI components
2. Implement match score display
3. Add game transition screens
4. Crawford game notifications
5. Match setup dialog

### Phase 5: Testing & Polish (Week 5)
1. End-to-end match testing
2. Performance optimization
3. Error handling improvements
4. Documentation updates

## Testing Strategy

### Unit Tests
- `MatchEngine` state transitions
- Crawford rule detection
- Point calculation scenarios
- Match completion logic

### Integration Tests
- Match persistence and retrieval
- Game-to-match relationships
- Concurrent match updates
- SignalR event flow

### E2E Tests
- Complete match flow
- Crawford game scenarios
- Network interruption handling
- UI state synchronization

### Test Scenarios
1. **Standard Match**: Play to completion without Crawford
2. **Crawford Activation**: Trigger Crawford rule and verify
3. **Post-Crawford**: Verify normal doubling after Crawford
4. **Gammon/Backgammon**: Verify correct point multiplication
5. **Disconnection**: Resume match after disconnect

## Migration Path

### Database Migration
1. Add match-related attributes to existing tables
2. No breaking changes to existing game structure
3. Single games continue to work without matches

### API Compatibility
1. All existing endpoints remain unchanged
2. New match endpoints are additive only
3. Gradual rollout with feature flags

### Client Updates
1. Detect server match support capability
2. Progressive enhancement approach
3. Fallback to single games if needed

### Feature Flags
```csharp
public class FeatureFlags
{
    public bool MatchPlayEnabled { get; set; } = false;
    public bool JacobyRuleAvailable { get; set; } = false;
    public bool BeaversEnabled { get; set; } = false;
}
```

## Future Enhancements

### Jacoby Rule
- No gammons count unless cube has been turned
- Optional setting per match
- UI indication when active

### Beavers
- Allow immediate redouble to 4x
- Optional advanced setting
- Requires additional UI controls

### Tournament Support
- Swiss/elimination brackets
- Multiple match management
- Leaderboards and rankings

### Match Statistics
- Win/loss records
- Average match length
- Crawford game statistics
- Doubling cube usage patterns