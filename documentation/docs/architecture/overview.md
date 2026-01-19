---
sidebar_position: 1
---

# Architecture Overview

DoubleCube.gg is a multi-project .NET solution with a React frontend. This page provides a high-level overview of the system architecture.

## System Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Frontend Clients (Browser)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │   Tab 1      │  │   Tab 2      │  │   Tab N      │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
                              │
                    SignalR WebSocket + REST API
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Backgammon.Server                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  GameHub     │  │  REST API    │  │  Services    │           │
│  │  (SignalR)   │  │  Endpoints   │  │  Layer       │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Backgammon.Core                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  GameEngine  │  │   Board      │  │  Match       │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────────┐
│                    Data Layer                                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐           │
│  │  DynamoDB    │  │  HybridCache │  │  Redis       │           │
│  │  (Storage)   │  │  (Caching)   │  │  (Backplane) │           │
│  └──────────────┘  └──────────────┘  └──────────────┘           │
└─────────────────────────────────────────────────────────────────┘
```

## Project Dependencies

```
Backgammon.AppHost (Aspire)
    └── Backgammon.Server
            ├── Backgammon.Core
            ├── Backgammon.AI
            ├── Backgammon.Analysis
            └── Backgammon.Plugins

Backgammon.WebClient (React)
    └── TypeScript client generated from IGameHub/IGameHubClient
```

## Key Design Principles

### 1. Server-Authoritative Logic

All game rules are enforced on the server in `Backgammon.Core`. The client cannot cheat because:
- Move validation happens server-side
- Dice rolls are generated on the server
- Game state is stored in the server's `GameSession`

### 2. Pure Domain Logic

`Backgammon.Core` has **zero dependencies**. It contains only:
- Game rules (movement, hitting, bearing off)
- Board representation
- Match scoring
- Doubling cube logic

This makes the core logic easy to test and reuse.

### 3. Real-Time Communication

SignalR provides bidirectional WebSocket communication:
- **Client → Server**: `invoke()` for actions (roll dice, make move)
- **Server → Client**: Events pushed to all connected tabs

### 4. Multi-Tab Support

A single player can have multiple browser tabs open:
- Each tab has its own connection ID
- Player tracked by `HashSet<string>` of connection IDs
- All tabs receive game updates
- Any tab can make moves

### 5. Single-Table DynamoDB Design

All entities stored in one DynamoDB table with composite keys:
- Efficient queries via GSIs
- Cost-effective (pay-per-request)
- Flexible schema for different entity types

## Communication Patterns

### Game Actions

1. Client calls `connection.invoke('MakeMove', from, to)`
2. Server validates move in `GameEngine`
3. Server broadcasts `GameUpdate` to all players' connections
4. All tabs update their UI

### Authentication

1. JWT token stored in localStorage
2. Token sent via query string for SignalR: `/gamehub?access_token=xxx`
3. Anonymous users created automatically on first visit

## Learn More

- [Core Engine](/architecture/core-engine) - Game logic details
- [Server Architecture](/architecture/server) - Backend services
- [Frontend Architecture](/architecture/frontend) - React application
