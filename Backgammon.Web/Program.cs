using Backgammon.Web.Hubs;
using Backgammon.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

// Add CORS for web clients (SignalR requires specific origins with credentials)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:3000",
                  "http://127.0.0.1:3000",
                  "http://localhost:5173",  // Vite dev server
                  "http://127.0.0.1:5173"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowAll");

// Map SignalR hub
app.MapHub<GameHub>("/gamehub");

// Health check endpoints
app.MapGet("/", () => "Backgammon SignalR Server Running - Connect via /gamehub");
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow });

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
});

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
