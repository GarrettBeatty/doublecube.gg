---
sidebar_position: 2
---

# Quick Start

Get the Backgammon application running in minutes.

## Using .NET Aspire (Recommended)

The easiest way to run the entire stack:

```bash
cd Backgammon.AppHost
dotnet run
```

This starts:
- **DynamoDB Local** - Database on port 8000
- **Redis** - Caching and SignalR backplane on port 6379
- **Backend Server** - ASP.NET Core on port 5000
- **Frontend** - Vite dev server on port 3000

Open [http://localhost:3000](http://localhost:3000) in your browser.

The Aspire Dashboard opens automatically with observability for all services.

## Manual Start

If you prefer to start services individually:

### Terminal 1: Start the Server

```bash
cd Backgammon.Server
dotnet run
```

Server runs on `http://localhost:5000`

### Terminal 2: Start the Web Client

```bash
cd Backgammon.WebClient
pnpm dev
```

Web UI runs on `http://localhost:3000`

## Playing a Game

1. Open [http://localhost:3000](http://localhost:3000)
2. You'll be automatically logged in as an anonymous user
3. Click **New Game** to create a match lobby
4. Open a second browser window/tab to join as the opponent
5. Roll dice, make moves, and enjoy!

## Console Mode

For a text-based experience:

```bash
cd Backgammon.Console
dotnet run
```

## AI Simulation

Watch AI players compete:

```bash
cd Backgammon.AI
dotnet run
```

## Next Steps

- [Development Guide](/getting-started/development) - Set up your dev workflow
- [Architecture](/architecture/overview) - Understand the system design
