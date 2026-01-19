---
sidebar_position: 1
slug: /
---

# Introduction

Welcome to the DoubleCube.gg documentation! This guide covers everything you need to know about the Backgammon multiplayer platform.

## What is DoubleCube.gg?

DoubleCube.gg is a complete implementation of the classic Backgammon board game built with .NET 10 and React. It supports:

- **Real-time multiplayer** via SignalR WebSocket connections
- **AI opponents** with multiple difficulty levels
- **Match play** with Crawford rule support
- **ELO rating system** for competitive play
- **Position analysis** with GNU Backgammon integration
- **Correspondence games** for asynchronous play

## Quick Start

The fastest way to get started is using .NET Aspire:

```bash
cd Backgammon.AppHost
dotnet run
```

This launches:
- DynamoDB Local (database)
- Redis (caching and SignalR backplane)
- Backend server (port 5000)
- Frontend dev server (port 3000)

Open [http://localhost:3000](http://localhost:3000) to start playing!

## Project Structure

```
doublecube.gg/
├── Backgammon.AppHost/         # .NET Aspire orchestrator
├── Backgammon.ServiceDefaults/ # Shared Aspire configuration
├── Backgammon.Core/            # Pure game logic (no dependencies)
├── Backgammon.Server/          # SignalR multiplayer server
├── Backgammon.WebClient/       # React + TypeScript frontend
├── Backgammon.AI/              # AI opponent framework
├── Backgammon.Analysis/        # Position evaluation
├── Backgammon.Plugins/         # Plugin system for bots and evaluators
├── Backgammon.Tests/           # xUnit test project
├── Backgammon.IntegrationTests/ # Integration tests
└── documentation/              # This documentation site
```

## Key Features

### Real-Time Multiplayer
Play against friends or random opponents with instant move updates via SignalR WebSocket connections.

### Multi-Tab Support
Open the same game in multiple browser tabs - all tabs stay synchronized and can make moves.

### AI Opponents
Challenge AI players ranging from random move selection to GNU Backgammon integration for expert-level play.

### Match Play
Play multi-game matches with proper scoring and Crawford rule enforcement.

### ELO Ratings
Track your skill progress with an ELO-based rating system.

### Daily Puzzles
Improve your game with daily tactical puzzles powered by GNU Backgammon analysis.

### Friend System
Add friends, challenge them to matches, and chat during games.

### Board Themes
Customize your board appearance with multiple themes.

## Next Steps

- [Installation Guide](/getting-started/installation) - Set up your development environment
- [Quick Start](/getting-started/quick-start) - Get a game running in minutes
- [Architecture Overview](/architecture/overview) - Understand how the system works
- [API Reference](/api/overview) - Explore the REST and SignalR APIs
