using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Backgammon.IntegrationTests.Fixtures;

/// <summary>
/// Provides a WebApplicationFactory for integration tests with SignalR support.
/// Configures the test server with DynamoDB Local and generates JWT tokens for authentication.
/// </summary>
public class WebApplicationFixture : IAsyncLifetime
{
    private const string JwtSecret = "LOCAL-DEV-ONLY-DO-NOT-USE-IN-PRODUCTION-CHANGE-ME-32CHARS!";
    private const string JwtIssuer = "BackgammonServer";
    private const string JwtAudience = "BackgammonClient";

    private WebApplicationFactory<Program>? _factory;
    private DynamoDbFixture? _dynamoDbFixture;

    /// <summary>
    /// Gets the HTTP client for making API requests.
    /// </summary>
    public HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    /// Gets the DynamoDB fixture for direct database access in tests.
    /// </summary>
    public DynamoDbFixture DynamoDb => _dynamoDbFixture ?? throw new InvalidOperationException("Fixture not initialized");

    /// <summary>
    /// Gets the service provider for resolving services in tests.
    /// </summary>
    public IServiceProvider Services => _factory?.Services ?? throw new InvalidOperationException("Factory not initialized");

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        // Start DynamoDB Local
        _dynamoDbFixture = new DynamoDbFixture();
        await _dynamoDbFixture.InitializeAsync();

        // Create WebApplicationFactory with test configuration
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Override configuration for tests
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = JwtSecret,
                        ["Jwt:Issuer"] = JwtIssuer,
                        ["Jwt:Audience"] = JwtAudience,
                        ["DynamoDb:TableName"] = _dynamoDbFixture.TableName
                    });
                });

                builder.ConfigureServices(services =>
                {
                    // Replace DynamoDB client with test container's client
                    services.RemoveAll<IAmazonDynamoDB>();
                    services.AddSingleton(_dynamoDbFixture.Client);

                    // Remove hosted services that might interfere with tests
                    services.RemoveAll<IHostedService>();

                    // Override JWT configuration - PostConfigure runs after initial setup
                    services.PostConfigure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                        Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                        options =>
                        {
                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
                            options.TokenValidationParameters = new TokenValidationParameters
                            {
                                ValidateIssuer = true,
                                ValidateAudience = true,
                                ValidateLifetime = true,
                                ValidateIssuerSigningKey = true,
                                ValidIssuer = JwtIssuer,
                                ValidAudience = JwtAudience,
                                IssuerSigningKey = key,
                                ClockSkew = TimeSpan.Zero
                            };
                        });
                });
            });

        HttpClient = _factory.CreateClient();
    }

    /// <inheritdoc/>
    public async Task DisposeAsync()
    {
        HttpClient?.Dispose();
        _factory?.Dispose();

        if (_dynamoDbFixture != null)
        {
            await _dynamoDbFixture.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a SignalR HubConnection with JWT authentication.
    /// </summary>
    /// <param name="userId">The user ID to include in the JWT token.</param>
    /// <param name="displayName">The display name for the user.</param>
    /// <returns>A configured HubConnection ready to start.</returns>
    public HubConnection CreateSignalRConnection(string userId, string displayName = "TestPlayer")
    {
        var token = GenerateJwtToken(userId, displayName);
        var handler = _factory!.Server.CreateHandler();

        return new HubConnectionBuilder()
            .WithUrl($"http://localhost/gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
    }

    /// <summary>
    /// Generates a valid JWT token for testing.
    /// </summary>
    /// <param name="userId">The user ID to include in the token.</param>
    /// <param name="displayName">The display name for the user.</param>
    /// <param name="expiresIn">Token expiration time (default 1 hour).</param>
    /// <returns>A valid JWT token string.</returns>
    public string GenerateJwtToken(string userId, string displayName = "TestPlayer", TimeSpan? expiresIn = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn ?? TimeSpan.FromHours(1)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates an expired JWT token for testing authentication failure.
    /// </summary>
    /// <param name="userId">The user ID to include in the token.</param>
    /// <returns>An expired JWT token string.</returns>
    public string GenerateExpiredJwtToken(string userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "TestPlayer"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: JwtIssuer,
            audience: JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1), // Already expired
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Registers an anonymous user via the API and returns the JWT token.
    /// This is required before creating a SignalR connection.
    /// </summary>
    /// <param name="displayName">The display name for the user.</param>
    /// <returns>A tuple containing the user ID and JWT token.</returns>
    public async Task<(string UserId, string Token)> RegisterAnonymousUserAsync(string displayName = "TestPlayer")
    {
        var playerId = $"anon_{Guid.NewGuid():N}";
        var request = new { PlayerId = playerId, DisplayName = displayName };

        var response = await HttpClient.PostAsJsonAsync("/api/auth/register-anonymous", request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AuthResult>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return (result!.User!.UserId!, result.Token!);
    }

    /// <summary>
    /// Creates a SignalR HubConnection with a pre-registered user.
    /// Registers the user in the database first, then creates the connection.
    /// </summary>
    /// <param name="displayName">The display name for the user.</param>
    /// <returns>A tuple containing the client, user ID, and the connection.</returns>
    public async Task<(string UserId, HubConnection Connection)> CreateRegisteredSignalRConnectionAsync(string displayName = "TestPlayer")
    {
        var (userId, token) = await RegisterAnonymousUserAsync(displayName);
        var handler = _factory!.Server.CreateHandler();

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/gamehub", options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        return (userId, connection);
    }

    private class AuthResult
    {
        public bool Success { get; set; }

        public string? Token { get; set; }

        public string? Error { get; set; }

        public UserResult? User { get; set; }
    }

    private class UserResult
    {
        public string? UserId { get; set; }

        public string? Username { get; set; }

        public string? DisplayName { get; set; }
    }
}
