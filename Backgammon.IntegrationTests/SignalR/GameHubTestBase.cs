using System.Security.Claims;
using Backgammon.Core;
using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Hubs;
using Backgammon.Server.Hubs.Interfaces;
using Backgammon.Server.Models;
using Backgammon.Server.Models.SignalR;
using Backgammon.Server.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backgammon.IntegrationTests.SignalR;

/// <summary>
/// Base class for GameHub integration tests.
/// Provides helpers for creating hubs with mocked SignalR context but real services.
/// Uses direct hub invocation pattern to bypass SignalR transport.
/// </summary>
public abstract class GameHubTestBase : IClassFixture<WebApplicationFixture>
{
    private readonly WebApplicationFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameHubTestBase"/> class.
    /// </summary>
    /// <param name="fixture">The web application fixture.</param>
    protected GameHubTestBase(WebApplicationFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Gets the web application fixture.
    /// </summary>
    protected WebApplicationFixture Fixture => _fixture;

    /// <summary>
    /// Gets captured GameState updates from GameUpdate broadcasts.
    /// </summary>
    protected List<GameState> CapturedGameUpdates { get; } = new();

    /// <summary>
    /// Gets captured GameState updates from GameStart broadcasts.
    /// </summary>
    protected List<GameState> CapturedGameStarts { get; } = new();

    /// <summary>
    /// Gets captured GameState updates from GameOver broadcasts.
    /// </summary>
    protected List<GameState> CapturedGameOvers { get; } = new();

    /// <summary>
    /// Gets captured error messages from Error broadcasts.
    /// </summary>
    protected List<string> CapturedErrors { get; } = new();

    /// <summary>
    /// Gets captured info messages from Info broadcasts.
    /// </summary>
    protected List<string> CapturedInfoMessages { get; } = new();

    /// <summary>
    /// Gets captured MatchCreated events.
    /// </summary>
    protected List<MatchCreatedDto> CapturedMatchCreated { get; } = new();

    /// <summary>
    /// Creates a ClaimsPrincipal with the specified user ID and display name.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>A ClaimsPrincipal with the specified claims.</returns>
    protected static ClaimsPrincipal CreateClaimsPrincipal(string userId, string displayName = "TestPlayer")
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("displayName", displayName)
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Creates a GameHub instance with mocked SignalR context but real DI services.
    /// </summary>
    /// <param name="userId">The user ID to authenticate as.</param>
    /// <param name="displayName">The display name for the user.</param>
    /// <returns>A tuple containing the configured hub and its connection ID.</returns>
    protected (GameHub Hub, string ConnectionId, Mock<IHubCallerClients<IGameHubClient>> MockClients)
        CreateHubForUser(string userId, string displayName = "TestPlayer")
    {
        var connectionId = Guid.NewGuid().ToString();

        // Get all real services from DI
        var sessionManager = _fixture.Services.GetRequiredService<IGameSessionManager>();
        var gameRepository = _fixture.Services.GetRequiredService<IGameRepository>();
        var aiMoveService = _fixture.Services.GetRequiredService<IAiMoveService>();
        var aiPlayerManager = _fixture.Services.GetRequiredService<IAiPlayerManager>();
        var eloRatingService = _fixture.Services.GetRequiredService<IEloRatingService>();
        var hubContext = _fixture.Services.GetRequiredService<IHubContext<GameHub, IGameHubClient>>();
        var matchService = _fixture.Services.GetRequiredService<IMatchService>();
        var playerConnectionService = _fixture.Services.GetRequiredService<IPlayerConnectionService>();
        var doubleOfferService = _fixture.Services.GetRequiredService<IDoubleOfferService>();
        var gameService = _fixture.Services.GetRequiredService<IGameService>();
        var playerProfileService = _fixture.Services.GetRequiredService<IPlayerProfileService>();
        var gameActionOrchestrator = _fixture.Services.GetRequiredService<IGameActionOrchestrator>();
        var playerStatsService = _fixture.Services.GetRequiredService<IPlayerStatsService>();
        var moveQueryService = _fixture.Services.GetRequiredService<IMoveQueryService>();
        var gameImportExportService = _fixture.Services.GetRequiredService<IGameImportExportService>();
        var chatService = _fixture.Services.GetRequiredService<IChatService>();
        var logger = _fixture.Services.GetRequiredService<ILogger<GameHub>>();
        var analysisService = _fixture.Services.GetRequiredService<AnalysisService>();
        var userRepository = _fixture.Services.GetRequiredService<IUserRepository>();
        var friendService = _fixture.Services.GetRequiredService<IFriendService>();
        var correspondenceGameService = _fixture.Services.GetRequiredService<ICorrespondenceGameService>();
        var authService = _fixture.Services.GetRequiredService<IAuthService>();
        var dailyPuzzleService = _fixture.Services.GetRequiredService<IDailyPuzzleService>();
        var gameCompletionService = _fixture.Services.GetRequiredService<IGameCompletionService>();

        // Create the hub with real services
        var hub = new GameHub(
            sessionManager,
            gameRepository,
            aiMoveService,
            aiPlayerManager,
            eloRatingService,
            hubContext,
            matchService,
            playerConnectionService,
            doubleOfferService,
            gameService,
            playerProfileService,
            gameActionOrchestrator,
            playerStatsService,
            moveQueryService,
            gameImportExportService,
            chatService,
            logger,
            analysisService,
            userRepository,
            friendService,
            correspondenceGameService,
            authService,
            dailyPuzzleService,
            gameCompletionService);

        // Mock the HubCallerContext with user claims
        var mockContext = new Mock<HubCallerContext>();
        mockContext.Setup(x => x.ConnectionId).Returns(connectionId);
        mockContext.Setup(x => x.User).Returns(CreateClaimsPrincipal(userId, displayName));

        // Create a mock for Groups (needed for JoinGame)
        var mockGroups = new Mock<IGroupManager>();
        mockGroups.Setup(x => x.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockGroups.Setup(x => x.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock the IHubCallerClients to capture broadcasts
        var mockCaller = new Mock<IGameHubClient>();
        ConfigureCallerMock(mockCaller);

        var mockClients = new Mock<IHubCallerClients<IGameHubClient>>();
        mockClients.Setup(x => x.Caller).Returns(mockCaller.Object);
        mockClients.Setup(x => x.All).Returns(mockCaller.Object);

        // Set up Group method to return a mock that captures group broadcasts
        mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(mockCaller.Object);

        // Set the mocked context and clients on the hub
        hub.Context = mockContext.Object;
        hub.Clients = mockClients.Object;
        hub.Groups = mockGroups.Object;

        return (hub, connectionId, mockClients);
    }

    /// <summary>
    /// Creates a test user in the database.
    /// </summary>
    /// <param name="displayName">The display name for the user.</param>
    /// <returns>The created user's ID.</returns>
    protected async Task<string> CreateTestUserAsync(string displayName = "TestPlayer")
    {
        var userId = $"test_{Guid.NewGuid():N}";
        var user = new User
        {
            UserId = userId,
            Username = displayName,
            UsernameNormalized = displayName.ToLowerInvariant(),
            DisplayName = displayName,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
            Stats = new UserStats(),
            IsActive = true
        };
        await GetUserRepository().CreateUserAsync(user);
        return userId;
    }

    /// <summary>
    /// Gets the user repository from DI.
    /// </summary>
    /// <returns>The user repository.</returns>
    protected IUserRepository GetUserRepository() =>
        _fixture.Services.GetRequiredService<IUserRepository>();

    /// <summary>
    /// Gets the match service from DI.
    /// </summary>
    /// <returns>The match service.</returns>
    protected IMatchService GetMatchService() =>
        _fixture.Services.GetRequiredService<IMatchService>();

    /// <summary>
    /// Gets the game session manager from DI.
    /// </summary>
    /// <returns>The game session manager.</returns>
    protected IGameSessionManager GetSessionManager() =>
        _fixture.Services.GetRequiredService<IGameSessionManager>();

    /// <summary>
    /// Gets the game repository from DI.
    /// </summary>
    /// <returns>The game repository.</returns>
    protected IGameRepository GetGameRepository() =>
        _fixture.Services.GetRequiredService<IGameRepository>();

    /// <summary>
    /// Clears all captured broadcasts. Call this at the start of each test.
    /// </summary>
    protected void ClearCapturedBroadcasts()
    {
        CapturedGameUpdates.Clear();
        CapturedGameStarts.Clear();
        CapturedGameOvers.Clear();
        CapturedErrors.Clear();
        CapturedInfoMessages.Clear();
        CapturedMatchCreated.Clear();
    }

    /// <summary>
    /// Configures the mock caller to capture broadcasts.
    /// </summary>
    /// <param name="mockCaller">The mock caller to configure.</param>
    private void ConfigureCallerMock(Mock<IGameHubClient> mockCaller)
    {
        mockCaller.Setup(x => x.GameUpdate(It.IsAny<GameState>()))
            .Callback<GameState>(state => CapturedGameUpdates.Add(state))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.GameStart(It.IsAny<GameState>()))
            .Callback<GameState>(state => CapturedGameStarts.Add(state))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.GameOver(It.IsAny<GameState>()))
            .Callback<GameState>(state => CapturedGameOvers.Add(state))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.Error(It.IsAny<string>()))
            .Callback<string>(msg => CapturedErrors.Add(msg))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.Info(It.IsAny<string>()))
            .Callback<string>(msg => CapturedInfoMessages.Add(msg))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.MatchCreated(It.IsAny<MatchCreatedDto>()))
            .Callback<MatchCreatedDto>(dto => CapturedMatchCreated.Add(dto))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.WaitingForOpponent(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.OpponentJoined(It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.OpponentLeft())
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.DoubleOffered(It.IsAny<DoubleOfferDto>()))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.DoubleAccepted(It.IsAny<GameState>()))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.MatchUpdate(It.IsAny<MatchUpdateDto>()))
            .Returns(Task.CompletedTask);

        mockCaller.Setup(x => x.LobbyCreated(It.IsAny<LobbyCreatedDto>()))
            .Returns(Task.CompletedTask);
    }
}
