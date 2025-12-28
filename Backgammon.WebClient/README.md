# Backgammon Web Client

A modern web-based client for playing Backgammon online via SignalR.

## Features

- 🎮 Real-time multiplayer gameplay
- 🎲 Interactive game board visualization
- 👥 Player status and turn indicators
- 📊 Live game statistics
- 📜 Event log for game actions
- 🎨 Responsive modern UI

## Running the Client

```bash
cd Backgammon.WebClient
dotnet run
```

The client will be available at `http://localhost:3000`

## Configuration

By default, the client connects to the SignalR server at `http://localhost:5000/gamehub`.

You can change this in the web UI by modifying the "Server URL" field.

## How to Play

1. **Start the SignalR Server**: Make sure `Backgammon.Server` is running on port 5000
2. **Open the Web Client**: Navigate to `http://localhost:3000`
3. **Connect**: Click "Connect" to establish connection with the server
4. **Join a Game**: Click "Join/Create Game" to start or join a game
5. **Play**: 
   - Roll dice when it's your turn
   - Enter moves (from point → to point)
   - End your turn when finished

## Architecture

This is a pure frontend client that communicates with the SignalR server via WebSockets.

- **HTML/CSS/JS** - No build process required
- **SignalR JavaScript Client** - Loaded from CDN
- **ASP.NET Core** - Minimal web server for serving static files

## Project Structure

```
Backgammon.WebClient/
├── Program.cs              # Web server configuration
├── wwwroot/
│   ├── index.html         # Main UI
│   ├── styles.css         # Styling
│   └── game.js            # SignalR client logic
└── Properties/
    └── launchSettings.json # Port configuration
```
