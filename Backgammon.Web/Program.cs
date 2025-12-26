using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Backgammon.Core;
using Backgammon.Web.Hubs;
using Backgammon.Web.Services;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

// MongoDB configuration with Aspire
// When running via Aspire, connection string comes from service discovery
// When running standalone, falls back to appsettings.json
builder.AddMongoDBClient("backgammon");
builder.Services.AddSingleton<IGameRepository, MongoGameRepository>();

// Add CORS for web clients (SignalR requires specific origins with credentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)  // Allow any origin in development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR
    });
});

var app = builder.Build();

// MUST be first - CORS middleware needs to run before Aspire endpoints
app.UseCors("AllowAll");

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map SignalR hub with CORS
app.MapHub<GameHub>("/gamehub").RequireCors("AllowAll");

// Health check endpoints
app.MapGet("/", () => "Backgammon SignalR Server Running - Connect via /gamehub").RequireCors("AllowAll");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow }).RequireCors("AllowAll");

// Game statistics endpoint
app.MapGet("/stats", (IGameSessionManager sessionManager) =>
{
    var games = sessionManager.GetAllGames().ToList();
    return new
    {
        totalGames = games.Count,
        activeGames = games.Count(g => g.IsFull && g.Engine.Winner == null),
        waitingGames = games.Count(g => !g.IsFull),
        completedGames = games.Count(g => g.Engine.Winner != null)
    };
}).RequireCors("AllowAll");

// Game list endpoint - returns list of games available to join
app.MapGet("/api/games", (IGameSessionManager sessionManager) =>
{
    var allGames = sessionManager.GetAllGames().ToList();
    
    return new
    {
        activeGames = allGames
            .Where(g => g.IsFull && g.Engine.Winner == null)
            .Select(g => new
            {
                gameId = g.Id,
                whitePlayer = g.WhitePlayerName ?? "Player 1",
                redPlayer = g.RedPlayerName ?? "Player 2",
                status = "playing",
                createdAt = g.CreatedAt
            })
            .ToList(),
        waitingGames = allGames
            .Where(g => !g.IsFull && g.Engine.Winner == null)
            .Select(g => new
            {
                gameId = g.Id,
                playerName = g.WhitePlayerName ?? g.RedPlayerName ?? "Waiting player",
                waitingSince = g.CreatedAt,
                minutesWaiting = (int)(DateTime.UtcNow - g.CreatedAt).TotalMinutes
            })
            .ToList()
    };
}).RequireCors("AllowAll");

// My games endpoint - returns active games for a specific player
app.MapGet("/api/player/{playerId}/active-games", (string playerId, IGameSessionManager sessionManager) =>
{
    var playerGames = sessionManager.GetPlayerGames(playerId).ToList();
    
    return playerGames.Select(g => new
    {
        gameId = g.Id,
        myColor = g.WhitePlayerId == playerId ? "White" : "Red",
        opponent = g.WhitePlayerId == playerId 
            ? (g.RedPlayerName ?? "Waiting for opponent") 
            : (g.WhitePlayerName ?? "Waiting for opponent"),
        isFull = g.IsFull,
        isMyTurn = g.Engine.CurrentPlayer?.Color == (g.WhitePlayerId == playerId ? CheckerColor.White : CheckerColor.Red),
        createdAt = g.CreatedAt,
        lastActivity = g.LastActivityAt
    }).ToList();
}).RequireCors("AllowAll");

// Database statistics endpoint
app.MapGet("/api/stats/db", async (IGameRepository gameRepository) =>
{
    var totalGames = await gameRepository.GetTotalGameCountAsync();
    var recentGames = await gameRepository.GetRecentGamesAsync(5);
    
    return new
    {
        totalCompletedGames = totalGames,
        recentGames = recentGames.Select(g => new
        {
            gameId = g.GameId,
            winner = g.Winner,
            stakes = g.Stakes,
            moveCount = g.MoveCount,
            duration = g.DurationSeconds,
            completedAt = g.CompletedAt
        })
    };
}).RequireCors("AllowAll");

// Player game history endpoint
app.MapGet("/api/player/{playerId}/games", async (string playerId, IGameRepository gameRepository, int limit = 20, int skip = 0) =>
{
    var games = await gameRepository.GetPlayerGamesAsync(playerId, limit, skip);
    return games;
}).RequireCors("AllowAll");

// Player statistics endpoint
app.MapGet("/api/player/{playerId}/stats", async (string playerId, IGameRepository gameRepository) =>
{
    var stats = await gameRepository.GetPlayerStatsAsync(playerId);
    return stats;
}).RequireCors("AllowAll");

// Get specific game by ID
app.MapGet("/api/game/{gameId}", async (string gameId, IGameRepository gameRepository) =>
{
    var game = await gameRepository.GetGameByIdAsync(gameId);
    if (game == null)
        return Results.NotFound(new { error = "Game not found" });
    
    return Results.Ok(game);
}).RequireCors("AllowAll");

// Cleanup background service for inactive games
var cleanupTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        var sessionManager = app.Services.GetRequiredService<IGameSessionManager>();
        sessionManager.CleanupInactiveGames(TimeSpan.FromHours(1));
    }
});

app.Run();

// Expose Program class for integration testing
public partial class Program { }
