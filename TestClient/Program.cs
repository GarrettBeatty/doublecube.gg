using Microsoft.AspNetCore.SignalR.Client;
using Backgammon.Core;

namespace Backgammon.Web.TestClient;

/// <summary>
/// Simple console client to test the SignalR server.
/// Demonstrates how any client can connect to the Backgammon server.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        var serverUrl = args.Length > 0 ? args[0] : "http://localhost:5000";
        
        Console.WriteLine("=== Backgammon SignalR Test Client ===");
        Console.WriteLine($"Connecting to {serverUrl}/gamehub...\n");
        
        var connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/gamehub")
            .WithAutomaticReconnect()
            .Build();
        
        // Register event handlers
        connection.On<dynamic>("GameStart", (gameState) =>
        {
            Console.WriteLine($"✓ Game Started!");
            Console.WriteLine($"  Game ID: {gameState.gameId}");
            var playerColor = gameState.whitePlayerId == connection.ConnectionId ? "White" : "Red";
            Console.WriteLine($"  You are: {playerColor}");
            Console.WriteLine($"  Current Player: {gameState.currentPlayer}");
            Console.WriteLine($"  Dice: [{string.Join(", ", gameState.dice)}]");
            Console.WriteLine();
        });
        
        connection.On<string>("WaitingForOpponent", (gameId) =>
        {
            Console.WriteLine($"⏳ Waiting for opponent... (Game ID: {gameId})");
            Console.WriteLine("   Tell a friend to join with this ID, or wait for matchmaking\n");
        });
        
        connection.On<dynamic>("DiceRolled", (gameState) =>
        {
            Console.WriteLine($"🎲 Dice Rolled: [{string.Join(", ", gameState.dice)}]");
            Console.WriteLine($"   Remaining moves: [{string.Join(", ", gameState.remainingMoves)}]");
            Console.WriteLine($"   Valid moves: {gameState.validMoves.Count}");
            Console.WriteLine();
        });
        
        connection.On<dynamic>("MoveMade", (gameState) =>
        {
            Console.WriteLine($"♟️  Move executed!");
            Console.WriteLine($"   Remaining moves: [{string.Join(", ", gameState.remainingMoves)}]");
            Console.WriteLine($"   White born off: {gameState.whiteBornOff}/15");
            Console.WriteLine($"   Red born off: {gameState.redBornOff}/15");
            Console.WriteLine();
        });
        
        connection.On<dynamic>("TurnEnded", (gameState) =>
        {
            Console.WriteLine($"✋ Turn ended. Current player: {gameState.currentPlayer}\n");
        });
        
        connection.On<dynamic>("GameOver", (gameState) =>
        {
            Console.WriteLine($"🏆 GAME OVER!");
            Console.WriteLine($"   Winner: {gameState.winner}");
            Console.WriteLine($"   Win Type: {gameState.winType}");
            Console.WriteLine();
        });
        
        connection.On("OpponentJoined", () =>
        {
            Console.WriteLine("✓ Opponent joined!\n");
        });
        
        connection.On("OpponentLeft", () =>
        {
            Console.WriteLine("⚠️  Opponent left the game\n");
        });
        
        connection.On<string>("Error", (message) =>
        {
            Console.WriteLine($"❌ Error: {message}\n");
        });
        
        // Connect
        try
        {
            await connection.StartAsync();
            Console.WriteLine("✓ Connected to server\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to connect: {ex.Message}");
            return;
        }
        
        // Interactive menu
        while (connection.State == HubConnectionState.Connected)
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  1) Join random game (matchmaking)");
            Console.WriteLine("  2) Join/create game by ID");
            Console.WriteLine("  3) Roll dice");
            Console.WriteLine("  4) Make move");
            Console.WriteLine("  5) End turn");
            Console.WriteLine("  6) Get game state");
            Console.WriteLine("  7) Leave game");
            Console.WriteLine("  0) Quit");
            Console.Write("\nChoice: ");
            
            var choice = Console.ReadLine();
            Console.WriteLine();
            
            try
            {
                switch (choice)
                {
                    case "1":
                        await connection.InvokeAsync("JoinGame");
                        Console.WriteLine("Joining matchmaking...\n");
                        break;
                    
                    case "2":
                        Console.Write("Enter game ID: ");
                        var gameId = Console.ReadLine();
                        await connection.InvokeAsync("JoinGame", gameId);
                        Console.WriteLine($"Joining game {gameId}...\n");
                        break;
                    
                    case "3":
                        await connection.InvokeAsync("RollDice");
                        break;
                    
                    case "4":
                        Console.Write("From point: ");
                        var from = int.Parse(Console.ReadLine() ?? "0");
                        Console.Write("To point: ");
                        var to = int.Parse(Console.ReadLine() ?? "0");
                        await connection.InvokeAsync("MakeMove", from, to);
                        break;
                    
                    case "5":
                        await connection.InvokeAsync("EndTurn");
                        break;
                    
                    case "6":
                        await connection.InvokeAsync("GetGameState");
                        break;
                    
                    case "7":
                        await connection.InvokeAsync("LeaveGame");
                        Console.WriteLine("Left game\n");
                        break;
                    
                    case "0":
                        await connection.StopAsync();
                        Console.WriteLine("Disconnected");
                        return;
                    
                    default:
                        Console.WriteLine("Invalid choice\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}\n");
            }
        }
    }
}
