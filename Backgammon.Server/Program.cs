using System.Linq;
using System.Security.Claims;
using System.Text;
using Backgammon.Core;
using Backgammon.Server.Configuration;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configure cache settings
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection(CacheSettings.SectionName));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>().Value);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddSignalR(options =>
{
    // Optimized timeouts for real-time gameplay
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);  // Client must respond within 60s
    options.KeepAliveInterval = TimeSpan.FromSeconds(20);      // Send keepalive pings every 20s
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);       // Keep at 30s
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.AddSingleton<IGameSessionManager, GameSessionManager>();

// Add memory cache for profile caching
builder.Services.AddMemoryCache();

// Add HybridCache for user profiles, game history, and friend lists
builder.Services.AddHybridCache(options =>
{
    options.MaximumPayloadBytes = 1024 * 1024; // 1MB
    options.MaximumKeyLength = 512;
    options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    };
});

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
builder.Services.AddSingleton<Backgammon.Server.Services.DynamoDb.DynamoDbUserRepository>();
builder.Services.AddSingleton<IUserRepository>(sp =>
{
    var dynamoDbUserRepo = sp.GetRequiredService<Backgammon.Server.Services.DynamoDb.DynamoDbUserRepository>();
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
    var cacheSettings = sp.GetRequiredService<CacheSettings>();
    var logger = sp.GetRequiredService<ILogger<CachedUserService>>();
    return new CachedUserService(dynamoDbUserRepo, cache, cacheSettings, logger);
});
builder.Services.AddSingleton<IFriendshipRepository, Backgammon.Server.Services.DynamoDb.DynamoDbFriendshipRepository>();
builder.Services.AddSingleton<IMatchRepository, Backgammon.Server.Services.DynamoDb.DynamoDbMatchRepository>();

// User and authentication services
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IFriendService, FriendService>();

// Match service
builder.Services.AddSingleton<IMatchService, MatchService>();

// AI opponent service
builder.Services.AddSingleton<IAiMoveService, AiMoveService>();

// Player connection tracking service
builder.Services.AddSingleton<IPlayerConnectionService, PlayerConnectionService>();

// Feature flags configuration
builder.Services.Configure<Backgammon.Server.Configuration.FeatureFlags>(builder.Configuration.GetSection("Features"));

// Bot game background service
builder.Services.AddHostedService<BotGameService>();

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
        policy.SetIsOriginAllowed(_ => true) // Allow any origin in development
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();  // Required for SignalR
    });

    options.AddPolicy("Production", policy =>
    {
        var domain = Environment.GetEnvironmentVariable("DOMAIN");
        if (!string.IsNullOrEmpty(domain))
        {
            // Parse comma-separated domains and allow both HTTP and HTTPS for each
            var domains = domain.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(d => d.Trim())
                                .SelectMany(d => new[] { $"http://{d}", $"https://{d}" })
                                .ToArray();

            policy.WithOrigins(domains)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();  // Required for SignalR
        }
        else
        {
            // Fallback to localhost if DOMAIN not set
            policy.WithOrigins("http://localhost:3000", "http://localhost")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
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
// Use Production CORS policy in production environment, AllowAll otherwise
var selectedCorsPolicy = app.Environment.IsProduction() ? "Production" : "AllowAll";
app.UseCors(selectedCorsPolicy);

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Map SignalR hub with CORS
app.MapHub<GameHub>("/gamehub").RequireCors(selectedCorsPolicy);

// Health check endpoints
app.MapGet("/", () => "Backgammon SignalR Server Running - Connect via /gamehub").RequireCors(selectedCorsPolicy);
app.MapGet("/health", () => new { status = "healthy", timestamp = DateTime.UtcNow }).RequireCors(selectedCorsPolicy);

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
}).RequireCors(selectedCorsPolicy);

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
}).RequireCors(selectedCorsPolicy);

// My games endpoint - returns active games for a specific player
app.MapGet("/api/player/{playerId}/active-games", async (string playerId, IGameRepository gameRepository) =>
{
    // Query database for all InProgress games (source of truth)
    var dbGames = await gameRepository.GetPlayerGamesAsync(playerId, "InProgress", limit: 50);

    return dbGames.Select(g => new
    {
        gameId = g.GameId,
        myColor = g.WhitePlayerId == playerId ? "White" : "Red",
        opponent = g.WhitePlayerId == playerId
            ? (g.RedPlayerName ?? "Waiting for opponent")
            : (g.WhitePlayerName ?? "Waiting for opponent"),
        isFull = !string.IsNullOrEmpty(g.WhitePlayerId) && !string.IsNullOrEmpty(g.RedPlayerId),
        isMyTurn = g.CurrentPlayer == (g.WhitePlayerId == playerId ? "White" : "Red"),
        createdAt = g.CreatedAt,
        lastActivity = g.LastUpdatedAt
    }).ToList();
}).RequireCors(selectedCorsPolicy);

// Bot games endpoint - returns active bot games for spectating
app.MapGet("/api/bot-games", (IGameSessionManager sessionManager) =>
{
    var botGames = sessionManager.GetAllGames()
        .Where(g => g.IsBotGame && !g.Engine.GameOver)
        .Select(g =>
        {
            var state = g.GetState(null);  // null = spectator view
            return new
            {
                gameId = g.Id,
                whitePlayer = g.WhitePlayerName,
                redPlayer = g.RedPlayerName,
                currentPlayer = g.Engine.CurrentPlayer?.Color.ToString() ?? "Unknown",
                whitePipCount = state.WhitePipCount,
                redPipCount = state.RedPipCount,
                status = state.Status.ToString(),
                spectatorCount = g.SpectatorConnections.Count
            };
        })
        .ToList();

    return botGames;
}).RequireCors(selectedCorsPolicy);

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
}).RequireCors(selectedCorsPolicy);

// Player game history endpoint (with caching)
app.MapGet("/api/player/{playerId}/games", async (
    string playerId,
    Microsoft.Extensions.Caching.Hybrid.HybridCache cache,
    CacheSettings cacheSettings,
    IGameRepository gameRepository,
    ILogger<Program> logger,
    int limit = 20,
    int skip = 0) =>
{
    var cacheKey = $"player:games:{playerId}:completed:limit={limit}:skip={skip}";

    try
    {
        var games = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                try
                {
                    return await gameRepository.GetPlayerGamesAsync(playerId, "Completed", limit, skip);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to fetch games for player {PlayerId}", playerId);
                    // Re-throw to prevent caching the error
                    // HybridCache does NOT cache exceptions - they propagate without creating a cache entry
                    throw;
                }
            },
            new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = cacheSettings.PlayerGames.Expiration,
                LocalCacheExpiration = cacheSettings.PlayerGames.LocalCacheExpiration
            },
            tags: [$"player:{playerId}"]);

        return Results.Ok(games);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving game history for player {PlayerId}", playerId);
        return Results.Problem("Failed to retrieve game history", statusCode: 500);
    }
}).RequireCors(selectedCorsPolicy);

// Player active matches endpoint
app.MapGet("/api/player/{playerId}/active-match", async (string playerId, IMatchRepository matchRepository) =>
{
    var matches = await matchRepository.GetPlayerMatchesAsync(playerId, "InProgress", limit: 1);
    var activeMatch = matches.FirstOrDefault();

    if (activeMatch == null)
    {
        return Results.Ok(new { hasActiveMatch = false });
    }

    return Results.Ok(new
    {
        hasActiveMatch = true,
        matchId = activeMatch.MatchId,
        targetScore = activeMatch.TargetScore,
        player1Id = activeMatch.Player1Id,
        player2Id = activeMatch.Player2Id,
        player1Score = activeMatch.Player1Score,
        player2Score = activeMatch.Player2Score,
        status = activeMatch.Status,
        currentGameId = activeMatch.CurrentGameId,
        isCrawfordGame = activeMatch.IsCrawfordGame,
        hasCrawfordGameBeenPlayed = activeMatch.HasCrawfordGameBeenPlayed
    });
}).RequireCors(selectedCorsPolicy);

// Player statistics endpoint (with caching)
app.MapGet("/api/player/{playerId}/stats", async (
    string playerId,
    Microsoft.Extensions.Caching.Hybrid.HybridCache cache,
    CacheSettings cacheSettings,
    IGameRepository gameRepository) =>
{
    var stats = await cache.GetOrCreateAsync(
        $"player:stats:{playerId}",
        async ct => await gameRepository.GetPlayerStatsAsync(playerId),
        new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
        {
            Expiration = cacheSettings.PlayerStats.Expiration,
            LocalCacheExpiration = cacheSettings.PlayerStats.LocalCacheExpiration
        },
        tags: [$"player:{playerId}"]);

    return stats;
}).RequireCors(selectedCorsPolicy);

// Get specific game by ID
app.MapGet("/api/game/{gameId}", async (string gameId, IGameRepository gameRepository) =>
{
    var game = await gameRepository.GetGameByGameIdAsync(gameId);
    if (game == null)
    {
        return Results.NotFound(new { error = "Game not found" });
    }

    return Results.Ok(game);
}).RequireCors(selectedCorsPolicy);

// ==================== AUTH ENDPOINTS ====================

// Register new user
app.MapPost("/api/auth/register", async (RegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAsync(request);
    return result.Success ? Results.Ok(result) : Results.BadRequest(result);
}).RequireCors(selectedCorsPolicy);

// Login
app.MapPost("/api/auth/login", async (LoginRequest request, IAuthService authService) =>
{
    var result = await authService.LoginAsync(request);
    return result.Success ? Results.Ok(result) : Results.Unauthorized();
}).RequireCors(selectedCorsPolicy);

// Get current user (requires auth)
app.MapGet("/api/auth/me", async (HttpContext context, IAuthService authService) =>
{
    var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var user = await authService.GetUserFromTokenAsync(token);
    return user != null ? Results.Ok(user) : Results.Unauthorized();
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// ==================== USER ENDPOINTS ====================

// Get user by ID
app.MapGet("/api/users/{userId}", async (string userId, IUserRepository userRepository) =>
{
    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound(new { error = "User not found" });
    }

    return Results.Ok(UserDto.FromUser(user));
}).RequireCors(selectedCorsPolicy);

// Update user profile (requires auth)
app.MapPut("/api/users/profile", async (UpdateProfileRequest request, HttpContext context, IUserRepository userRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
    {
        return Results.NotFound(new { error = "User not found" });
    }

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

    // Update privacy settings
    if (request.ProfilePrivacy.HasValue)
    {
        user.ProfilePrivacy = request.ProfilePrivacy.Value;
    }

    if (request.GameHistoryPrivacy.HasValue)
    {
        user.GameHistoryPrivacy = request.GameHistoryPrivacy.Value;
    }

    if (request.FriendsListPrivacy.HasValue)
    {
        user.FriendsListPrivacy = request.FriendsListPrivacy.Value;
    }

    await userRepository.UpdateUserAsync(user);
    return Results.Ok(UserDto.FromUser(user));
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Search users (requires auth)
app.MapGet("/api/users/search", async (string q, IUserRepository userRepository) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
    {
        return Results.BadRequest(new { error = "Search query must be at least 2 characters" });
    }

    var users = await userRepository.SearchUsersAsync(q, 10);
    return Results.Ok(users.Select(u => new
    {
        userId = u.UserId,
        username = u.Username,
        displayName = u.DisplayName
    }));
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// ==================== FRIEND ENDPOINTS ====================

// Get friends list (requires auth)
app.MapGet("/api/friends", async (HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var friends = await friendService.GetFriendsAsync(userId);
    return Results.Ok(friends);
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Get pending friend requests (requires auth)
app.MapGet("/api/friends/requests", async (HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var requests = await friendService.GetPendingRequestsAsync(userId);
    return Results.Ok(requests);
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Send friend request (requires auth)
app.MapPost("/api/friends/request/{toUserId}", async (string toUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.SendFriendRequestAsync(userId, toUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Accept friend request (requires auth)
app.MapPost("/api/friends/accept/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.AcceptFriendRequestAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Decline friend request (requires auth)
app.MapPost("/api/friends/decline/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.DeclineFriendRequestAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Remove friend (requires auth)
app.MapDelete("/api/friends/{friendUserId}", async (string friendUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.RemoveFriendAsync(userId, friendUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Block user (requires auth)
app.MapPost("/api/friends/block/{blockedUserId}", async (string blockedUserId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.BlockUserAsync(userId, blockedUserId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Invite friend to game (requires auth)
app.MapPost("/api/friends/invite/{friendUserId}/game/{gameId}", async (string friendUserId, string gameId, HttpContext context, IFriendService friendService) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var (success, error) = await friendService.InviteFriendToGameAsync(userId, friendUserId, gameId);
    return success ? Results.Ok() : Results.BadRequest(new { error });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Cleanup background service for inactive games
var cleanupTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(30));

        var sessionManager = app.Services.GetRequiredService<IGameSessionManager>();
        var gameRepository = app.Services.GetRequiredService<IGameRepository>();

        // Evict from memory after 6 hours of inactivity (but keep in DB as "InProgress")
        var allGames = sessionManager.GetAllGames();
        var evictionCutoff = DateTime.UtcNow - TimeSpan.FromHours(6);
        var inactiveGames = allGames
            .Where(g => g.LastActivityAt < evictionCutoff && g.Engine.Winner == null)
            .ToList();

        foreach (var game in inactiveGames)
        {
            try
            {
                // Save current state before eviction
                await gameRepository.SaveGameAsync(GameEngineMapper.ToGame(game));

                // Remove from memory (status stays "InProgress" in DB!)
                sessionManager.RemoveGame(game.Id);

                Console.WriteLine($"[Cleanup] Evicted game {game.Id} from memory (still resumable in DB)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Failed to evict game {game.Id}: {ex.Message}");
            }
        }

        // True abandonment: 90 days without activity
        var abandonmentCutoff = DateTime.UtcNow - TimeSpan.FromDays(90);
        var abandonedGames = await gameRepository.GetGamesLastUpdatedBeforeAsync(abandonmentCutoff, "InProgress");

        foreach (var abandonedGame in abandonedGames)
        {
            try
            {
                await gameRepository.UpdateGameStatusAsync(abandonedGame.GameId, "Abandoned");
                Console.WriteLine($"[Cleanup] Marked game {abandonedGame.GameId} as Abandoned (90+ days inactive)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Cleanup] Failed to mark game {abandonedGame.GameId} as abandoned: {ex.Message}");
            }
        }
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
public partial class Program
{
}
