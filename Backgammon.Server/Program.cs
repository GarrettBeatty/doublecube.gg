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
using Backgammon.Server.Endpoints;
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

// Analysis sessions live on IAnalysisSessionGrain (per-user); no singleton needed.

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

// Chat lives on IMatchChatGrain (per-match); no service registration needed.

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

// Resource endpoints are organized by concern under Endpoints/. Each module owns
// its CORS + auth wiring; the Hub and Aspire-mapped routes remain inline above.
app.MapHealthEndpoints(selectedCorsPolicy);
app.MapGameEndpoints(selectedCorsPolicy);
app.MapPlayerEndpoints(selectedCorsPolicy);
app.MapBotEndpoints(selectedCorsPolicy);
app.MapAuthEndpoints(selectedCorsPolicy);
app.MapUserEndpoints(selectedCorsPolicy);
app.MapFriendEndpoints(selectedCorsPolicy);
app.MapThemeEndpoints(selectedCorsPolicy);


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
