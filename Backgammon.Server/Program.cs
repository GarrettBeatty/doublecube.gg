using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Backgammon.AI;
using Backgammon.AI.Extensions;
using Backgammon.Analysis.Extensions;
using Backgammon.Core;
using Backgammon.Plugins.Extensions;
using Backgammon.Plugins.Registration;
using Backgammon.Server.Configuration;
using Backgammon.Server.Data;
using Backgammon.Server.Hubs;
using Backgammon.Server.Models;
using Backgammon.Server.Services;
using Backgammon.Server.Services.Postgres;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Orleans;

var builder = WebApplication.CreateBuilder(args);

// Configure cache settings
builder.Services.Configure<CacheSettings>(
    builder.Configuration.GetSection(CacheSettings.SectionName));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CacheSettings>>().Value);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// ========== ORLEANS CONFIGURATION ==========
// Grain state is persisted via AdoNet against the same Postgres instance used for game history.
// The Orleans schema (OrleansQuery, OrleansStorage) is bootstrapped at startup by
// OrleansSchemaInitializer.EnsureSchemaAsync before the silo activates any grain.
//
// Clustering is injected by Aspire when running under AppHost; standalone runs fall back
// to localhost clustering so dotnet run still works.
var postgresConnectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Postgres connection string is required for Orleans grain persistence");

builder.UseOrleans(silo =>
{
    if (string.IsNullOrEmpty(builder.Configuration["Orleans:ClusterId"]))
    {
        silo.UseLocalhostClustering();
    }

    silo.AddAdoNetGrainStorageAsDefault(options =>
    {
        options.Invariant = "Npgsql";
        options.ConnectionString = postgresConnectionString;
    });

    // PubSubStore is used by Orleans Streams (not currently used in this app). Keep in
    // memory — switching to persistent here would require additional schema and gives
    // us nothing today.
    silo.AddMemoryGrainStorage("PubSubStore");
});
// ========== END ORLEANS CONFIGURATION ==========

// Add services to the container
// Aspire injects as ConnectionStrings__redis; fall back to manual appsettings key
var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? builder.Configuration["Redis:ConnectionString"];
// Register authentication filter globally for all SignalR hubs
builder.Services.AddSingleton<IHubFilter, Backgammon.Server.Hubs.Filters.AuthenticationHubFilter>();

var signalRBuilder = builder.Services.AddSignalR(options =>
{
    // Optimized timeouts for real-time gameplay
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);  // Client must respond within 60s
    options.KeepAliveInterval = TimeSpan.FromSeconds(20);      // Send keepalive pings every 20s
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);       // Keep at 30s
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Add Redis backplane for scaling across multiple server instances
// This enables SignalR messages to be broadcast across all servers
if (!string.IsNullOrEmpty(redisConnectionString))
{
    Console.WriteLine($"=== Configuring SignalR Redis Backplane ===");
    Console.WriteLine($"Redis Connection: {redisConnectionString}");
    signalRBuilder.AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration.ChannelPrefix = StackExchange.Redis.RedisChannel.Literal("BackgammonSignalR");
    });
    Console.WriteLine("SignalR Redis backplane configured");
    Console.WriteLine("========================================\n");
}
else
{
    Console.WriteLine("=== SignalR Running in Single-Server Mode ===");
    Console.WriteLine("WARNING: Redis not configured. Real-time updates will NOT work across multiple server instances.");
    Console.WriteLine("Set Redis:ConnectionString in configuration to enable backplane.");
    Console.WriteLine("=============================================\n");
}

builder.Services.AddSingleton<IAnalysisSessionManager, AnalysisSessionManager>();

// Add memory cache for profile caching
builder.Services.AddMemoryCache();

// Add Redis distributed cache for HybridCache L2 (distributed) layer
// This enables cache sharing across multiple server instances
if (!string.IsNullOrEmpty(redisConnectionString))
{
    Console.WriteLine($"=== Configuring Redis Distributed Cache for HybridCache ===");
    Console.WriteLine($"Redis Connection: {redisConnectionString}");
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "BackgammonCache:";
    });
    Console.WriteLine("Redis distributed cache configured for HybridCache");
    Console.WriteLine("=========================================================\n");
}
else
{
    Console.WriteLine("=== HybridCache Running with L1 (Memory) Only ===");
    Console.WriteLine("WARNING: Redis not configured. Cache will NOT be shared across multiple server instances.");
    Console.WriteLine("Set Redis:ConnectionString in configuration to enable distributed caching.");
    Console.WriteLine("==================================================\n");
}

// Add HybridCache for user profiles, game history, and friend lists
// HybridCache automatically uses the configured IDistributedCache (Redis) as L2
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

// ========== POSTGRESQL CONFIGURATION ==========
// DbContextFactory is used instead of DbContext so that repositories can create
// short-lived contexts per operation (safe for concurrent use and long-lived services).
builder.Services.AddDbContextFactory<BackgammonDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.EnableRetryOnFailure(maxRetryCount: 3)));

// Register PostgreSQL repositories
builder.Services.AddSingleton<IGameRepository, PostgresGameRepository>();
builder.Services.AddSingleton<IFriendshipRepository, PostgresFriendshipRepository>();
builder.Services.AddSingleton<IMatchRepository, PostgresMatchRepository>();
builder.Services.AddSingleton<IThemeRepository, PostgresThemeRepository>();
builder.Services.AddSingleton<IPuzzleRepository, PostgresPuzzleRepository>();

// User repository is wrapped in a caching layer
builder.Services.AddSingleton<PostgresUserRepository>();
builder.Services.AddSingleton<IUserRepository>(sp =>
{
    var pgUserRepo = sp.GetRequiredService<PostgresUserRepository>();
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Hybrid.HybridCache>();
    var cacheSettings = sp.GetRequiredService<CacheSettings>();
    var logger = sp.GetRequiredService<ILogger<CachedUserService>>();
    return new CachedUserService(pgUserRepo, cache, cacheSettings, logger);
});
// ========== END POSTGRESQL CONFIGURATION ==========

// User and authentication services
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IFriendService, FriendService>();

// Match service
builder.Services.AddSingleton<IMatchService, MatchService>();

// Correspondence game service
builder.Services.AddSingleton<ICorrespondenceGameService, CorrespondenceGameService>();

// AI opponent service
builder.Services.AddSingleton<IBotResolver, BotResolver>();
builder.Services.AddSingleton<IAiMoveService, AiMoveService>();

// AI player management for matches (ensures consistent AI across match continuations)
builder.Services.AddSingleton<IAiPlayerManager, AiPlayerManager>();

// Presence tracking is owned by the singleton IPresenceGrain ("global"); resolved via IGrainFactory.

builder.Services.AddSingleton<IPlayerProfileService, PlayerProfileService>();
builder.Services.AddSingleton<IPlayerStatsService, PlayerStatsService>();

builder.Services.AddSingleton<IMatchChatStorage, MatchChatStorage>();
builder.Services.AddSingleton<IChatService, ChatService>();

// ========== ANALYSIS CONFIGURATION ==========
// Configure analysis settings
builder.Services.Configure<Backgammon.Analysis.Configuration.AnalysisSettings>(
    builder.Configuration.GetSection(Backgammon.Analysis.Configuration.AnalysisSettings.SectionName));
builder.Services.Configure<Backgammon.Analysis.Configuration.GnubgSettings>(
    builder.Configuration.GetSection(Backgammon.Analysis.Configuration.GnubgSettings.SectionName));

// Register PositionEvaluatorFactory for per-request evaluator selection
// Note: GnubgProcessManager is registered by AddAnalysisPlugins() below
builder.Services.AddSingleton<PositionEvaluatorFactory>();

// Register AnalysisService (now uses factory for evaluator)
builder.Services.AddSingleton<IAnalysisService, AnalysisService>();
// ========== END ANALYSIS CONFIGURATION ==========

// ========== PLUGIN SYSTEM CONFIGURATION ==========
// Register the plugin system infrastructure
builder.Services.AddBackgammonPlugins(builder.Configuration);

// Register all bots — add new bots in Backgammon.AI/BotRegistrations.cs
builder.Services.AddAllBots();

// Register analysis evaluators (heuristic always; gnubg when service is available)
builder.Services.AddAnalysisPlugins(includeGnubg: true);
// ========== END PLUGIN SYSTEM CONFIGURATION ==========

// ========== DAILY PUZZLE CONFIGURATION ==========
// Configure puzzle settings
builder.Services.Configure<Backgammon.Server.Configuration.PuzzleSettings>(
    builder.Configuration.GetSection(Backgammon.Server.Configuration.PuzzleSettings.SectionName));

// Register puzzle repository
builder.Services.AddSingleton<IPuzzleRepository, PostgresPuzzleRepository>();

// Register puzzle services
builder.Services.AddSingleton<RandomPositionGenerator>();
builder.Services.AddSingleton<IDailyPuzzleService, DailyPuzzleService>();

// Register puzzle generation background service
builder.Services.AddHostedService<DailyPuzzleGenerationService>();
// ========== END DAILY PUZZLE CONFIGURATION ==========

// ELO rating service
builder.Services.AddSingleton<IEloRatingService, EloRatingService>();

// Feature flags configuration
builder.Services.Configure<Backgammon.Server.Configuration.FeatureFlags>(builder.Configuration.GetSection("Features"));

// Bot game background service
builder.Services.AddHostedService<BotGameService>();

// ========== SWAGGER/OPENAPI CONFIGURATION ==========
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Add SignalR hub documentation
    options.AddSignalRSwaggerGen();

    // Include XML comments for API documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
// ========== END SWAGGER CONFIGURATION ==========

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

// Enable Swagger UI in development and production (read-only documentation)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Backgammon API v1");
    c.RoutePrefix = "swagger";
});

// Apply any pending EF Core migrations on startup
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackgammonDbContext>();
    await db.Database.MigrateAsync();
}

// Bootstrap Orleans grain storage schema (idempotent — uses IF NOT EXISTS and ON CONFLICT)
await OrleansSchemaInitializer.EnsureSchemaAsync(
    postgresConnectionString,
    app.Services.GetRequiredService<ILogger<Program>>());

// Seed default themes
Console.WriteLine("=== Seeding default themes ===");
var themeRepository = app.Services.GetRequiredService<IThemeRepository>();
await DefaultThemeSeeder.SeedDefaultThemesAsync(themeRepository);
Console.WriteLine("=== Theme seeding complete ===\n");

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
app.MapGet("/stats", async (IGameRepository gameRepository) =>
{
    var totalGames = await gameRepository.GetTotalGameCountAsync(null);
    var activeGamesCount = await gameRepository.GetTotalGameCountAsync("InProgress");
    var completedGamesCount = await gameRepository.GetTotalGameCountAsync("Completed");
    var abandonedGamesCount = await gameRepository.GetTotalGameCountAsync("Abandoned");

    return new
    {
        totalGames,
        activeGames = activeGamesCount,
        completedGames = completedGamesCount,
        abandonedGames = abandonedGamesCount
    };
}).RequireCors(selectedCorsPolicy);

// Game list endpoint - returns list of active games from DB
app.MapGet("/api/games", async (IGameRepository gameRepository) =>
{
    var dbActiveGames = await gameRepository.GetActiveGamesAsync();
    var activeGamesList = dbActiveGames.Select(g => new
    {
        gameId = g.GameId,
        whitePlayer = g.WhitePlayerName ?? "Player 1",
        redPlayer = g.RedPlayerName ?? "Player 2",
        whiteUsername = g.WhitePlayerName,
        redUsername = g.RedPlayerName,
        status = "playing",
        createdAt = g.CreatedAt
    }).ToList();

    return new { activeGames = activeGamesList };
}).RequireCors(selectedCorsPolicy);

// My games endpoint - returns active games for a specific player (DB-only, grains auto-activate)
app.MapGet("/api/player/{playerId}/active-games", async (string playerId, IGameRepository gameRepository) =>
{
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
    })
    .OrderByDescending(g => g.lastActivity)
    .ToList();
}).RequireCors(selectedCorsPolicy);

// Bot games endpoint - returns active bot games for spectating (DB-based)
app.MapGet("/api/bot-games", async (IGameRepository gameRepository) =>
{
    var botGames = await gameRepository.GetActiveGamesAsync();
    return botGames
        .Where(g => g.IsAiOpponent)
        .Select(g => new
        {
            gameId = g.GameId,
            whitePlayer = g.WhitePlayerName,
            redPlayer = g.RedPlayerName,
            currentPlayer = g.CurrentPlayer ?? "Unknown",
            status = g.Status
        })
        .ToList();
}).RequireCors(selectedCorsPolicy);

// Available bots endpoint - returns list of registered AI bots
app.MapGet("/api/bots", (IPluginRegistry registry) =>
{
    var bots = registry.GetAvailableBots()
        .Select(b => new
        {
            botId = b.BotId,
            displayName = b.DisplayName,
            description = b.Description,
            estimatedElo = b.EstimatedElo,
            requiresExternalResources = b.RequiresExternalResources
        })
        .ToList();

    return bots;
}).RequireCors(selectedCorsPolicy);

// Available evaluators endpoint - returns list of registered position evaluators
app.MapGet("/api/evaluators", (IPluginRegistry registry) =>
{
    var evaluators = registry.GetAvailableEvaluators()
        .Select(e => new
        {
            evaluatorId = e.EvaluatorId,
            displayName = e.DisplayName,
            requiresExternalResources = e.RequiresExternalResources
        })
        .ToList();

    return evaluators;
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

// Register anonymous user
app.MapPost("/api/auth/register-anonymous", async (AnonymousRegisterRequest request, IAuthService authService) =>
{
    var result = await authService.RegisterAnonymousUserAsync(request.PlayerId, request.DisplayName);
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

// ==================== THEME ENDPOINTS ====================

// Get public themes (paginated)
app.MapGet("/api/themes", async (IThemeRepository themeRepository, int limit = 50, string? cursor = null) =>
{
    var (themes, nextCursor) = await themeRepository.GetPublicThemesAsync(limit, cursor);
    return Results.Ok(new { themes, nextCursor });
}).RequireCors(selectedCorsPolicy);

// Get default themes
app.MapGet("/api/themes/defaults", async (IThemeRepository themeRepository) =>
{
    var themes = await themeRepository.GetDefaultThemesAsync();
    return Results.Ok(themes);
}).RequireCors(selectedCorsPolicy);

// Get theme by ID
app.MapGet("/api/themes/{themeId}", async (string themeId, IThemeRepository themeRepository) =>
{
    var theme = await themeRepository.GetByIdAsync(themeId);
    if (theme == null)
    {
        return Results.NotFound(new { error = "Theme not found" });
    }

    return Results.Ok(theme);
}).RequireCors(selectedCorsPolicy);

// Get current user's created themes (requires auth)
app.MapGet("/api/themes/my", async (HttpContext context, IThemeRepository themeRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var themes = await themeRepository.GetThemesByAuthorAsync(userId);
    return Results.Ok(themes);
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Create new theme (requires auth)
app.MapPost("/api/themes", async (Backgammon.Server.Models.BoardTheme theme, HttpContext context, IThemeRepository themeRepository, IUserRepository userRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    theme.ThemeId = Guid.NewGuid().ToString();
    theme.AuthorId = userId;
    theme.AuthorUsername = user.Username;
    theme.IsDefault = false;
    theme.CreatedAt = DateTime.UtcNow;
    theme.UpdatedAt = DateTime.UtcNow;
    theme.UsageCount = 0;
    theme.LikeCount = 0;

    await themeRepository.CreateThemeAsync(theme);
    return Results.Created($"/api/themes/{theme.ThemeId}", theme);
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Update theme (requires auth, author only)
app.MapPut("/api/themes/{themeId}", async (string themeId, Backgammon.Server.Models.BoardTheme updatedTheme, HttpContext context, IThemeRepository themeRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var existingTheme = await themeRepository.GetByIdAsync(themeId);
    if (existingTheme == null)
    {
        return Results.NotFound(new { error = "Theme not found" });
    }

    if (existingTheme.AuthorId != userId)
    {
        return Results.Forbid();
    }

    if (existingTheme.IsDefault)
    {
        return Results.BadRequest(new { error = "Cannot modify default themes" });
    }

    existingTheme.Name = updatedTheme.Name;
    existingTheme.Description = updatedTheme.Description;
    existingTheme.Visibility = updatedTheme.Visibility;
    existingTheme.Colors = updatedTheme.Colors;
    existingTheme.UpdatedAt = DateTime.UtcNow;

    await themeRepository.UpdateThemeAsync(existingTheme);
    return Results.Ok(existingTheme);
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Delete theme (requires auth, author only)
app.MapDelete("/api/themes/{themeId}", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var theme = await themeRepository.GetByIdAsync(themeId);
    if (theme == null)
    {
        return Results.NotFound(new { error = "Theme not found" });
    }

    if (theme.AuthorId != userId)
    {
        return Results.Forbid();
    }

    if (theme.IsDefault)
    {
        return Results.BadRequest(new { error = "Cannot delete default themes" });
    }

    await themeRepository.DeleteThemeAsync(themeId);
    return Results.Ok();
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Like a theme (requires auth)
app.MapPost("/api/themes/{themeId}/like", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var theme = await themeRepository.GetByIdAsync(themeId);
    if (theme == null)
    {
        return Results.NotFound(new { error = "Theme not found" });
    }

    await themeRepository.LikeThemeAsync(themeId, userId);
    return Results.Ok();
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Unlike a theme (requires auth)
app.MapDelete("/api/themes/{themeId}/like", async (string themeId, HttpContext context, IThemeRepository themeRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    await themeRepository.UnlikeThemeAsync(themeId, userId);
    return Results.Ok();
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Get user's theme preference (requires auth)
app.MapGet("/api/themes/preference", async (HttpContext context, IUserRepository userRepository) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new { selectedThemeId = user.SelectedThemeId });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Set user's theme preference (requires auth)
app.MapPut("/api/themes/preference", async (HttpContext context, IUserRepository userRepository, IThemeRepository themeRepository, string? themeId) =>
{
    var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userId))
    {
        return Results.Unauthorized();
    }

    var user = await userRepository.GetByUserIdAsync(userId);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    // Validate theme exists if provided
    if (!string.IsNullOrEmpty(themeId))
    {
        var theme = await themeRepository.GetByIdAsync(themeId);
        if (theme == null)
        {
            return Results.NotFound(new { error = "Theme not found" });
        }

        // Track usage count changes
        if (!string.IsNullOrEmpty(user.SelectedThemeId) && user.SelectedThemeId != themeId)
        {
            await themeRepository.DecrementUsageCountAsync(user.SelectedThemeId);
        }

        if (user.SelectedThemeId != themeId)
        {
            await themeRepository.IncrementUsageCountAsync(themeId);
        }
    }
    else if (!string.IsNullOrEmpty(user.SelectedThemeId))
    {
        // Switching to default (null theme)
        await themeRepository.DecrementUsageCountAsync(user.SelectedThemeId);
    }

    user.SelectedThemeId = themeId;
    await userRepository.UpdateUserAsync(user);
    return Results.Ok(new { selectedThemeId = themeId });
}).RequireAuthorization().RequireCors(selectedCorsPolicy);

// Search themes
app.MapGet("/api/themes/search", async (string q, IThemeRepository themeRepository) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
    {
        return Results.BadRequest(new { error = "Search query must be at least 2 characters" });
    }

    var themes = await themeRepository.SearchThemesAsync(q);
    return Results.Ok(themes);
}).RequireCors(selectedCorsPolicy);

// Cleanup background service for stale DB records
var cleanupTask = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(TimeSpan.FromMinutes(30));

        var gameRepository = app.Services.GetRequiredService<IGameRepository>();

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

// Expose Program class for integration testing
public partial class Program
{
}
