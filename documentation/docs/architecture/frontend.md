---
sidebar_position: 4
---

# Frontend Architecture

The `Backgammon.WebClient` project is a React + TypeScript application built with Vite.

## Technology Stack

- **React 18** - UI framework
- **TypeScript 5** - Type safety
- **Vite 7** - Build tool
- **TailwindCSS** - Styling
- **shadcn/ui** - Component library
- **Zustand** - State management
- **SignalR** - Real-time communication

## Project Structure

```
src/
├── components/
│   ├── game/           # Game UI (BoardSVG, GameControls)
│   ├── friends/        # Social features
│   ├── modals/         # Dialog components
│   └── ui/             # shadcn/ui components
├── contexts/
│   ├── AuthContext.tsx
│   ├── SignalRContext.tsx
│   └── MatchContext.tsx
├── hooks/
│   └── useSignalREvents.ts
├── pages/
│   ├── HomePage.tsx
│   ├── GamePage.tsx
│   └── ProfilePage.tsx
├── services/
│   ├── signalr.service.ts
│   ├── auth.service.ts
│   └── api.service.ts
├── stores/
│   └── gameStore.ts
└── types/
    └── signalr.generated.ts
```

## State Management

### Zustand Store

Global game state managed via Zustand:

```typescript
const useGameStore = create<GameStore>((set) => ({
  currentGameState: null,
  myColor: null,
  validMoves: [],
  isAnalysisMode: false,

  setGameState: (state) => set({ currentGameState: state }),
  setMyColor: (color) => set({ myColor: color }),
  // ...
}));
```

### React Contexts

- **AuthContext**: User authentication state
- **SignalRContext**: Connection management
- **MatchContext**: Current match state

## SignalR Integration

### Connection Setup

```typescript
const connection = new HubConnectionBuilder()
  .withUrl(`${serverUrl}/gamehub?access_token=${token}`)
  .withAutomaticReconnect()
  .build();
```

### Event Handling

The `useSignalREvents` hook registers all event handlers:

```typescript
function useSignalREvents(gameId: string) {
  const connection = useSignalRContext();
  const gameStateRef = useRef(useGameStore.getState);

  useEffect(() => {
    const handlers = {
      GameUpdate: (state: GameState) => {
        if (state.gameId === gameId) {
          useGameStore.setState({ currentGameState: state });
        }
      },
      // ... more handlers
    };

    Object.entries(handlers).forEach(([event, handler]) => {
      connection.on(event, handler);
    });

    return () => {
      Object.keys(handlers).forEach(event => {
        connection.off(event);
      });
    };
  }, [connection, gameId]);
}
```

### Invoking Server Methods

```typescript
await connection.invoke('MakeMove', fromPoint, toPoint);
await connection.invoke('RollDice');
await connection.invoke('EndTurn');
```

## Key Components

### BoardSVG

Interactive SVG board that renders:
- 24 points with checkers
- Bar and bear-off areas
- Move indicators and highlights
- Drag-and-drop support

### GameControls

Turn action buttons:
- Roll Dice
- End Turn
- Undo Move
- Offer Double

### PlayerCard

Displays player information:
- Name and rating
- Timer (if time controls enabled)
- Connection status indicator

## Type Safety

TypeScript types are generated from the server's SignalR interfaces:

```bash
pnpm generate:signalr
```

This creates `signalr.generated.ts` with:
- `IGameHub` method signatures
- `IGameHubClient` event types
- All DTO types

## Build Process

### Development

```bash
pnpm dev
```

Vite dev server with HMR on port 3000.

### Production

```bash
pnpm build
```

Output to `wwwroot/` directory, served by the .NET backend.
