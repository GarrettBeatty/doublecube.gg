using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Backgammon.Core;
using Backgammon.Server.Hubs;
using Backgammon.Server.Services;
using Backgammon.Server.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

// ========== DYNAMODB CONFIGURATION ==========
Console.WriteLine("=== DynamoDB Configuration ===");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

var dynamoDbTableName = builder.Configuration["DynamoDb:TableName"];
var awsEndpointUrl = Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL_DYNAMODB");
var awsRegion = builder.Configuration["AWS:Region"] ?? Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1";

Console.WriteLine($"DynamoDb:TableName = {dynamoDbTableName ?? "NULL"}");
Console.WriteLine($"AWS_ENDPOINT_URL_DYNAMODB = {awsEndpointUrl ?? "NULL"}");
Console.WriteLine($"AWS_REGION = {awsRegion}");
Console.WriteLine("=====================================\n");
// ========== END CONFIGURATION ==========

// DynamoDB client configuration
// AWS SDK automatically detects local endpoint via AWS_ENDPOINT_URL_DYNAMODB environment variable
builder.Services.AddSingleton<Amazon.DynamoDBv2.IAmazonDynamoDB>(sp =>
{
    // No configuration needed - AWS SDK automatically picks up:
    // - AWS_ENDPOINT_URL_DYNAMODB for local development (set by Aspire)
    // - AWS credentials and region from environment/config
    return new Amazon.DynamoDBv2.AmazonDynamoDBClient();
});


// Register DynamoDB services
builder.Services.AddSingleton<IGameRepository, Backgammon.Server.Services.DynamoDb.DynamoDbGameRepository>();
builder.Services.AddSingleton<IUserRepository, Backgammon.Server.Services.DynamoDb.DynamoDbUserRepository>();
builder.Services.AddSingleton<IFriendshipRepository, Backgammon.Server.Services.DynamoDb.DynamoDbFriendshipRepository>();

// User and authentication services
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IFriendService, FriendService>();

// JWT Authentication configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BackgammonServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BackgammonClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // Support token in query string for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gamehub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

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

// Initialize DynamoDB table if needed (local development only)
if (!string.IsNullOrEmpty(awsEndpointUrl))
{
    Console.WriteLine("=== Initializing DynamoDB table ===");
    var dynamoDb = app.Services.GetRequiredService<Amazon.DynamoDBv2.IAmazonDynamoDB>();
    await EnsureTableExistsAsync(dynamoDb, dynamoDbTableName ?? "backgammon-local");
    Console.WriteLine("=== DynamoDB initialization complete ===\n");
}

// Load active games from database on startup (restore from previous session)
Console.WriteLine("=== Loading active games from database ===");
var sessionManager = app.Services.GetRequiredService<IGameSessionManager>();
var gameRepository = app.Services.GetRequiredService<IGameRepository>();
await sessionManager.LoadActiveGamesAsync(gameRepository);
Console.WriteLine("=== Game loading complete ===\n");

// MUST be first - CORS middleware needs to run before Aspire endpoints
app.UseCors("AllowAll");

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

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
    var totalGames = await gameRepository.GetTotalGameCountAsync("Completed");
    var recentGames = await gameRepository.GetRecentGamesAsync("Completed", 5);

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
    var games = await gameRepository.GetPlayerGamesAsync(playerId, "Completed", limit, skip);
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
    var game = await gameRepository.GetGameByGameIdAsync(gameId);
    if (game == null)
        return Results.NotFound(new { error = "Game not found" });

    return Results.Ok(game);
}).RequireCors("AllowAll");

// ==================== AUTH ENDPOINTS ====================

// Register new user
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}).RequireCors("AllowAll");

// Login
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result) : Results.Unauthorized();
}).RequireCors("AllowAll");

// Get current user (requires auth)
app.MapGet("/api/auth/me", async (HttpContext context, IAuthService authService) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
        return Results.Unauthorized();

    var user = await authService.GetUserFromTokenAsync(token);
    return user != null ? Results.Ok(user) : Results.Unauthorized();
}).RequireAuthorization().RequireCors("AllowAll");

// ==================== USER ENDPOINTS ====================

// Get user by ID
app.MapGet("/api/users/{userId}", async (string userId, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
        return Results.NotFound(new { error = "User not found" });

    return Results.Ok(UserDto.FromUser(user));
}).RequireCors("AllowAll");

// Update user profile (requires auth)
app.MapPut("/api/users/profile", async (UpdateProfileRequest request, HttpContext context, IUserRepository userRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
        return Results.NotFound(new { error = "User not found" });

    if (!string.IsNullOrWhiteSpace(request.DisplayName))
    {
        user.DisplayName = request.DisplayName;
    }

    if (!string.IsNullOrWhiteSpace(request.Email))
    {
        // Check if email is already taken
        if (await userRepository.EmailExistsAsync(request.Email) &&
            user.EmailNormalized != request.Email.ToLowerInvariant())
        {
            return Results.BadRequest(new { error = "Email already in use" });
        }
        user.Email = request.Email;
        user.EmailNormalized = request.Email.ToLowerInvariant();
    }

    await userRepository.UpdateUserAsync(user);
    return Results.Ok(UserDto.FromUser(user));
}).RequireAuthorization().RequireCors("AllowAll");

// Search users (requires auth)
app.MapGet("/api/users/search", async (string q, IUserRepository userRepository) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Results.BadRequest(new { error = "Search query must be at least 2 characters" });

    var users = await userRepository.SearchUsersAsync(q, 10);
    return Results.Ok(users.Select(u => new
    {
        userId = u.UserId,
        username = u.Username,
        displayName = u.DisplayName
    }));
}).RequireAuthorization().RequireCors("AllowAll");

// ==================== FRIEND ENDPOINTS ====================

// Get friends list (requires auth)
app.MapGet("/api/friends", async (HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var friends = await friendService.GetFriendsAsync(userId);
    return Results.Ok(friends);
}).RequireAuthorization().RequireCors("AllowAll");

// Get pending friend requests (requires auth)
app.MapGet("/api/friends/requests", async (HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var requests = await friendService.GetPendingRequestsAsync(userId);
    return Results.Ok(requests);
}).RequireAuthorization().RequireCors("AllowAll");

// Send friend request (requires auth)
app.MapPost("/api/friends/request/{toUserId}", async (string toUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.SendFriendRequestAsync(userId, toUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Accept friend request (requires auth)
app.MapPost("/api/friends/accept/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.AcceptFriendRequestAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Decline friend request (requires auth)
app.MapPost("/api/friends/decline/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.DeclineFriendRequestAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Remove friend (requires auth)
app.MapDelete("/api/friends/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.RemoveFriendAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Block user (requires auth)
app.MapPost("/api/friends/block/{blockedUserId}", async (string blockedUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.BlockUserAsync(userId, blockedUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Invite friend to game (requires auth)
app.MapPost("/api/friends/invite/{friendUserId}/game/{gameId}", async (string friendUserId, string gameId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
        return Results.Unauthorized();

    var (success, error) = await friendService.InviteFriendToGameAsync(userId, friendUserId, gameId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors("AllowAll");

// Cleanup background service for inactive games
var cleanupTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(5));

        var sessionManager = app.Services.GetRequiredService<IGameSessionManager>();
        var gameRepository = app.Services.GetRequiredService<IGameRepository>();

        // Get all games before cleanup to identify abandoned ones
        var allGames = sessionManager.GetAllGames();
        var cutoff = DateTime.UtcNow - TimeSpan.FromHours(1);
        var inactiveGames = allGames.Where(g => g.LastActivityAt < cutoff && g.Engine.Winner == null).ToList();

        // Mark abandoned games in database
        foreach (var abandonedGame in inactiveGames)
        {
            try
            {
                await gameRepository.UpdateGameStatusAsync(abandonedGame.Id, "Abandoned");
                Console.WriteLine($"[Cleanup] Marked game {abandonedGame.Id} as Abandoned");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Failed to mark game {abandonedGame.Id} as abandoned: {ex.Message}");
            }
        }

        // Clean up from memory
        sessionManager.CleanupInactiveGames(TimeSpan.FromHours(1));
    }
});

app.Run();

// Helper method to ensure DynamoDB table exists (local development only)
static async Task EnsureTableExistsAsync(Amazon.DynamoDBv2.IAmazonDynamoDB dynamoDb, string tableName)
{
    try
    {
        // Check if table exists
        await dynamoDb.DescribeTableAsync(tableName);
        Console.WriteLine($"Table '{tableName}' already exists");
        return;
    }
    catch (Amazon.DynamoDBv2.Model.ResourceNotFoundException)
    {
        Console.WriteLine($"Table '{tableName}' not found, creating...");
    }

    // Create table with single-table design
    var request = new Amazon.DynamoDBv2.Model.CreateTableRequest
    {
        TableName = tableName,
        KeySchema = new List<Amazon.DynamoDBv2.Model.KeySchemaElement>
        {
            new() { AttributeName = "PK", KeyType = Amazon.DynamoDBv2.KeyType.HASH },
            new() { AttributeName = "SK", KeyType = Amazon.DynamoDBv2.KeyType.RANGE }
        },
        AttributeDefinitions = new List<Amazon.DynamoDBv2.Model.AttributeDefinition>
        {
            new() { AttributeName = "PK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "SK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI1PK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI1SK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI2PK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI2SK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI3PK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S },
            new() { AttributeName = "GSI3SK", AttributeType = Amazon.DynamoDBv2.ScalarAttributeType.S }
        },
        BillingMode = Amazon.DynamoDBv2.BillingMode.PAY_PER_REQUEST,
        GlobalSecondaryIndexes = new List<Amazon.DynamoDBv2.Model.GlobalSecondaryIndex>
        {
            new()
            {
                IndexName = "GSI1",
                KeySchema = new List<Amazon.DynamoDBv2.Model.KeySchemaElement>
                {
                    new() { AttributeName = "GSI1PK", KeyType = Amazon.DynamoDBv2.KeyType.HASH },
                    new() { AttributeName = "GSI1SK", KeyType = Amazon.DynamoDBv2.KeyType.RANGE }
                },
                Projection = new Amazon.DynamoDBv2.Model.Projection { ProjectionType = Amazon.DynamoDBv2.ProjectionType.ALL }
            },
            new()
            {
                IndexName = "GSI2",
                KeySchema = new List<Amazon.DynamoDBv2.Model.KeySchemaElement>
                {
                    new() { AttributeName = "GSI2PK", KeyType = Amazon.DynamoDBv2.KeyType.HASH },
                    new() { AttributeName = "GSI2SK", KeyType = Amazon.DynamoDBv2.KeyType.RANGE }
                },
                Projection = new Amazon.DynamoDBv2.Model.Projection { ProjectionType = Amazon.DynamoDBv2.ProjectionType.ALL }
            },
            new()
            {
                IndexName = "GSI3",
                KeySchema = new List<Amazon.DynamoDBv2.Model.KeySchemaElement>
                {
                    new() { AttributeName = "GSI3PK", KeyType = Amazon.DynamoDBv2.KeyType.HASH },
                    new() { AttributeName = "GSI3SK", KeyType = Amazon.DynamoDBv2.KeyType.RANGE }
                },
                Projection = new Amazon.DynamoDBv2.Model.Projection { ProjectionType = Amazon.DynamoDBv2.ProjectionType.ALL }
            }
        }
    };

    await dynamoDb.CreateTableAsync(request);
    Console.WriteLine($"Table '{tableName}' created successfully");

    // Wait for table to become active
    for (int i = 0; i < 30; i++)
    {
        await Task.Delay(1000);
        var response = await dynamoDb.DescribeTableAsync(tableName);
        if (response.Table.TableStatus == Amazon.DynamoDBv2.TableStatus.ACTIVE)
        {
            Console.WriteLine($"Table '{tableName}' is now active");
            return;
        }
    }
}

// Expose Program class for integration testing
public partial class Program { }
