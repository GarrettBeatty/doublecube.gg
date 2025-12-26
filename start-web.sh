#!/bin/bash

# Backgammon Web Multiplayer Quick Start
# This script starts both the SignalR server and web client

echo "🎲 Starting Backgammon Multiplayer..."
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK not found. Please install .NET 10.0 or later."
    exit 1
fi

# Build projects
echo "🔨 Building projects..."
dotnet build Backgammon.sln --configuration Release > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please check for errors."
    exit 1
fi

echo "✅ Build successful!"
echo ""

# Kill any existing processes on ports 5000 and 3000
echo "🧹 Cleaning up existing processes..."
lsof -ti:5000 | xargs kill -9 2>/dev/null || true
lsof -ti:3000 | xargs kill -9 2>/dev/null || true
sleep 1

echo ""
echo "🚀 Starting servers..."
echo ""

# Start SignalR server in background
echo "   📡 SignalR Server starting on http://localhost:5000"
cd Backgammon.Web
dotnet run --no-build --configuration Release > /dev/null 2>&1 &
SERVER_PID=$!
cd ..

# Wait for server to start
sleep 2

# Start web client in background
echo "   🌐 Web Client starting on http://localhost:3000"
cd Backgammon.WebClient
dotnet run --no-build --configuration Release > /dev/null 2>&1 &
CLIENT_PID=$!
cd ..

# Wait for client to start
sleep 2

echo ""
echo "✅ Both servers are running!"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎮 Open your browser and navigate to:"
echo ""
echo "   👉  http://localhost:3000"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📝 To play:"
echo "   1. Click 'Connect' in the web interface"
echo "   2. Click 'Join/Create Game'"
echo "   3. Open another browser tab/window and repeat"
echo "   4. Start playing!"
echo ""
echo "🛑 Press Ctrl+C to stop both servers"
echo ""

# Function to cleanup on exit
cleanup() {
    echo ""
    echo "🛑 Shutting down servers..."
    kill $SERVER_PID 2>/dev/null
    kill $CLIENT_PID 2>/dev/null
    lsof -ti:5000 | xargs kill -9 2>/dev/null || true
    lsof -ti:3000 | xargs kill -9 2>/dev/null || true
    echo "✅ Cleanup complete. Goodbye!"
    exit 0
}

# Trap Ctrl+C
trap cleanup SIGINT SIGTERM

# Wait for user to stop
wait
