#!/bin/bash
# Test the SignalR client by joining a game, getting state, and leaving

cd "$(dirname "$0")"

echo "=== Testing Backgammon SignalR Client ===" 
echo ""
echo "This will:"
echo "1. Connect to server"
echo "2. Join a game with ID 'demo-game'"
echo "3. Get game state"
echo "4. Leave game"
echo "5. Quit"
echo ""
echo "Press Enter to start..."
read

# Use printf to send commands
printf "2\ndemo-game\n6\n7\n0\n" | dotnet run

echo ""
echo "Test complete!"
