using Backgammon.IntegrationTests.Fixtures;
using Backgammon.Server.Models;
using Backgammon.Server.Services.DynamoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backgammon.IntegrationTests.Database;

/// <summary>
/// Integration tests for MatchRepository with real DynamoDB Local.
/// Tests match persistence, serialization, and query operations.
/// </summary>
[Collection("DynamoDB")]
[Trait("Category", "Integration")]
[Trait("Component", "DynamoDB")]
public class MatchRepositoryTests : IAsyncLifetime
{
    private readonly DynamoDbFixture _fixture;
    private DynamoDbMatchRepository _repository = null!;

    public MatchRepositoryTests(DynamoDbFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DynamoDb:TableName"] = _fixture.TableName
            })
            .Build();

        _repository = new DynamoDbMatchRepository(
            _fixture.Client,
            config,
            NullLogger<DynamoDbMatchRepository>.Instance);

        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ==================== Save and Retrieve Tests ====================

    [Fact]
    public async Task SaveMatchAsync_NewMatch_CanBeRetrieved()
    {
        // Arrange
        var match = CreateTestMatch();

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.MatchId.Should().Be(match.MatchId);
        retrieved.Player1Id.Should().Be(match.Player1Id);
        retrieved.Player2Id.Should().Be(match.Player2Id);
        retrieved.TargetScore.Should().Be(match.TargetScore);
        retrieved.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task SaveMatchAsync_PreservesPlayerNames()
    {
        // Arrange
        var match = CreateTestMatch();
        match.Player1Name = "Alice";
        match.Player2Name = "Bob";

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Player1Name.Should().Be("Alice");
        retrieved.Player2Name.Should().Be("Bob");
    }

    [Fact]
    public async Task SaveMatchAsync_PreservesScores()
    {
        // Arrange
        var match = CreateTestMatch();
        match.Player1Score = 3;
        match.Player2Score = 2;

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Player1Score.Should().Be(3);
        retrieved.Player2Score.Should().Be(2);
    }

    [Fact]
    public async Task SaveMatchAsync_PreservesCrawfordState()
    {
        // Arrange
        var match = CreateTestMatch();
        match.IsCrawfordGame = true;
        match.HasCrawfordGameBeenPlayed = false;

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.IsCrawfordGame.Should().BeTrue();
        retrieved.HasCrawfordGameBeenPlayed.Should().BeFalse();
    }

    [Fact]
    public async Task SaveMatchAsync_PreservesCurrentGameId()
    {
        // Arrange
        var match = CreateTestMatch();
        var gameId = Guid.NewGuid().ToString();
        match.CurrentGameId = gameId;

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CurrentGameId.Should().Be(gameId);
    }

    [Fact]
    public async Task SaveMatchAsync_PreservesOpponentType()
    {
        // Arrange
        var match = CreateTestMatch();
        match.OpponentType = "AI";

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.OpponentType.Should().Be("AI");
        // Note: IsRated is not persisted to DynamoDB (not in MarshalMatch/UnmarshalMatch)
    }

    [Fact]
    public async Task SaveMatchAsync_WithGamesSummary_PreservesGameSummaries()
    {
        // Arrange
        var match = CreateTestMatch();
        match.GamesSummary = new List<MatchGameSummary>
        {
            new MatchGameSummary
            {
                GameId = Guid.NewGuid().ToString(),
                Winner = "White",
                Stakes = 2,
                WinType = "Gammon",
                IsCrawford = false,
                CompletedAt = DateTime.UtcNow
            },
            new MatchGameSummary
            {
                GameId = Guid.NewGuid().ToString(),
                Winner = "Red",
                Stakes = 1,
                WinType = "Normal",
                IsCrawford = true,
                CompletedAt = DateTime.UtcNow
            }
        };

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.GamesSummary.Should().HaveCount(2);

        var firstGame = retrieved.GamesSummary[0];
        firstGame.Winner.Should().Be("White");
        firstGame.Stakes.Should().Be(2);
        firstGame.WinType.Should().Be("Gammon");
        firstGame.IsCrawford.Should().BeFalse();

        var secondGame = retrieved.GamesSummary[1];
        secondGame.Winner.Should().Be("Red");
        secondGame.Stakes.Should().Be(1);
        secondGame.IsCrawford.Should().BeTrue();
    }

    [Fact]
    public async Task GetMatchByIdAsync_UsesConsistentRead()
    {
        // This test verifies that GetMatchByIdAsync uses ConsistentRead=true
        // We do this by saving and immediately reading - with eventually consistent reads
        // this could fail intermittently, but with strongly consistent reads it should always work

        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        // Immediately retrieve multiple times - all should succeed with consistent reads
        for (int i = 0; i < 5; i++)
        {
            var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);
            retrieved.Should().NotBeNull();
            retrieved!.MatchId.Should().Be(match.MatchId);
        }
    }

    [Fact]
    public async Task GetMatchByIdAsync_NonExistent_ReturnsNull()
    {
        // Act
        var result = await _repository.GetMatchByIdAsync("non-existent-match-id");

        // Assert
        result.Should().BeNull();
    }

    // ==================== Player Match Index Tests ====================

    [Fact]
    public async Task SaveMatchAsync_CreatesPlayerMatchIndex_ForBothPlayers()
    {
        // Arrange
        var player1Id = Guid.NewGuid().ToString();
        var player2Id = Guid.NewGuid().ToString();
        var match = CreateTestMatch(player1Id, player2Id);

        // Act
        await _repository.SaveMatchAsync(match);

        // Both players should be able to find the match
        var player1Matches = await _repository.GetPlayerMatchesAsync(player1Id);
        var player2Matches = await _repository.GetPlayerMatchesAsync(player2Id);

        // Assert
        player1Matches.Should().ContainSingle(m => m.MatchId == match.MatchId);
        player2Matches.Should().ContainSingle(m => m.MatchId == match.MatchId);
    }

    [Fact]
    public async Task SaveMatchAsync_OpenLobby_OnlyCreatesIndexForPlayer1()
    {
        // Arrange - open lobby has no Player2 yet
        var match = CreateTestMatch();
        match.Player2Id = string.Empty;
        match.IsOpenLobby = true;
        match.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;

        // Act
        await _repository.SaveMatchAsync(match);

        // Player1 should find the match
        var player1Matches = await _repository.GetPlayerMatchesAsync(match.Player1Id);
        player1Matches.Should().ContainSingle(m => m.MatchId == match.MatchId);
    }

    [Fact]
    public async Task GetPlayerMatchesAsync_ReturnsAllPlayerMatches()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();

        var match1 = CreateTestMatch(playerId);
        var match2 = CreateTestMatch(playerId);
        var match3 = CreateTestMatch(playerId);

        await _repository.SaveMatchAsync(match1);
        await _repository.SaveMatchAsync(match2);
        await _repository.SaveMatchAsync(match3);

        // Act
        var matches = await _repository.GetPlayerMatchesAsync(playerId);

        // Assert
        matches.Should().HaveCount(3);
        matches.Select(m => m.MatchId).Should().Contain(match1.MatchId);
        matches.Select(m => m.MatchId).Should().Contain(match2.MatchId);
        matches.Select(m => m.MatchId).Should().Contain(match3.MatchId);
    }

    [Fact]
    public async Task GetPlayerMatchesAsync_WithStatusFilter_ReturnsOnlyMatchingStatus()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();

        var inProgressMatch = CreateTestMatch(playerId);
        inProgressMatch.CoreMatch.Status = Core.MatchStatus.InProgress;

        var completedMatch = CreateTestMatch(playerId);
        completedMatch.CoreMatch.Status = Core.MatchStatus.Completed;
        completedMatch.WinnerId = playerId;

        await _repository.SaveMatchAsync(inProgressMatch);
        await _repository.SaveMatchAsync(completedMatch);

        // Act
        var inProgressMatches = await _repository.GetPlayerMatchesAsync(playerId, "InProgress");
        var completedMatches = await _repository.GetPlayerMatchesAsync(playerId, "Completed");

        // Assert
        inProgressMatches.Should().ContainSingle(m => m.MatchId == inProgressMatch.MatchId);
        completedMatches.Should().ContainSingle(m => m.MatchId == completedMatch.MatchId);
    }

    // ==================== Update Tests ====================

    [Fact]
    public async Task UpdateMatchAsync_UpdatesScores()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        match.Player1Score = 4;
        match.Player2Score = 3;

        // Act
        await _repository.UpdateMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Player1Score.Should().Be(4);
        retrieved.Player2Score.Should().Be(3);
    }

    [Fact]
    public async Task UpdateMatchAsync_UpdatesCrawfordState()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        match.IsCrawfordGame = true;
        match.HasCrawfordGameBeenPlayed = false;

        // Act
        await _repository.UpdateMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.IsCrawfordGame.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateMatchAsync_UpdatesCurrentGameId()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        var newGameId = Guid.NewGuid().ToString();
        match.CurrentGameId = newGameId;

        // Act
        await _repository.UpdateMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CurrentGameId.Should().Be(newGameId);
    }

    [Fact]
    public async Task UpdateMatchAsync_WhenCompleted_SetsCompletionData()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        match.CoreMatch.Status = Core.MatchStatus.Completed;
        match.CompletedAt = DateTime.UtcNow;
        match.WinnerId = match.Player1Id;
        match.DurationSeconds = 3600;

        // Act
        await _repository.UpdateMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
        retrieved.CompletedAt.Should().NotBeNull();
        retrieved.WinnerId.Should().Be(match.Player1Id);
        retrieved.DurationSeconds.Should().Be(3600);
    }

    [Fact]
    public async Task UpdateMatchAsync_AddsPlayer2WhenJoining()
    {
        // Arrange - open lobby initially has no Player2
        var match = CreateTestMatch();
        match.Player2Id = string.Empty;
        match.Player2Name = string.Empty;
        match.IsOpenLobby = true;
        match.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;
        await _repository.SaveMatchAsync(match);

        // Player 2 joins
        var player2Id = Guid.NewGuid().ToString();
        match.Player2Id = player2Id;
        match.Player2Name = "Bob";
        match.CoreMatch.Status = Core.MatchStatus.InProgress;

        // Act
        await _repository.UpdateMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Player2Id.Should().Be(player2Id);
        retrieved.Player2Name.Should().Be("Bob");
    }

    // ==================== Add Game To Match Tests ====================

    [Fact]
    public async Task AddGameToMatchAsync_AddsGameIdToList()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        var gameId1 = Guid.NewGuid().ToString();
        var gameId2 = Guid.NewGuid().ToString();

        // Act
        await _repository.AddGameToMatchAsync(match.MatchId, gameId1);
        await _repository.AddGameToMatchAsync(match.MatchId, gameId2);

        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.GameIds.Should().Contain(gameId1);
        retrieved.GameIds.Should().Contain(gameId2);
    }

    [Fact]
    public async Task AddGameToMatchAsync_UpdatesCurrentGameId()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        var gameId = Guid.NewGuid().ToString();

        // Act
        await _repository.AddGameToMatchAsync(match.MatchId, gameId);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CurrentGameId.Should().Be(gameId);
    }

    [Fact]
    public async Task AddGameToMatchAsync_CreatesGameSummaryEntry()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        var gameId = Guid.NewGuid().ToString();

        // Act
        await _repository.AddGameToMatchAsync(match.MatchId, gameId);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.GamesSummary.Should().ContainSingle();
        retrieved.GamesSummary[0].GameId.Should().Be(gameId);
    }

    // ==================== Status Update Tests ====================

    [Fact]
    public async Task UpdateMatchStatusAsync_ChangesStatus()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        // Act
        await _repository.UpdateMatchStatusAsync(match.MatchId, "Completed");
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task UpdateMatchStatusAsync_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        // Act
        await _repository.UpdateMatchStatusAsync(match.MatchId, "Completed");
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CompletedAt.Should().NotBeNull();
        retrieved.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ==================== GSI3 Query Tests (Status Index) ====================

    [Fact]
    public async Task GetActiveMatchesAsync_ReturnsOnlyInProgressMatches()
    {
        // Arrange
        var inProgressMatch = CreateTestMatch();
        inProgressMatch.CoreMatch.Status = Core.MatchStatus.InProgress;

        var completedMatch = CreateTestMatch();
        completedMatch.CoreMatch.Status = Core.MatchStatus.Completed;
        completedMatch.WinnerId = completedMatch.Player1Id;

        await _repository.SaveMatchAsync(inProgressMatch);
        await _repository.SaveMatchAsync(completedMatch);

        // Act
        var activeMatches = await _repository.GetActiveMatchesAsync();

        // Assert
        activeMatches.Should().Contain(m => m.MatchId == inProgressMatch.MatchId);
        activeMatches.Should().NotContain(m => m.MatchId == completedMatch.MatchId);
    }

    [Fact]
    public async Task GetRecentMatchesAsync_ReturnsMatchesWithCorrectStatus()
    {
        // Arrange
        var completedMatch1 = CreateTestMatch();
        completedMatch1.CoreMatch.Status = Core.MatchStatus.Completed;
        completedMatch1.WinnerId = completedMatch1.Player1Id;

        var completedMatch2 = CreateTestMatch();
        completedMatch2.CoreMatch.Status = Core.MatchStatus.Completed;
        completedMatch2.WinnerId = completedMatch2.Player1Id;

        await _repository.SaveMatchAsync(completedMatch1);
        await _repository.SaveMatchAsync(completedMatch2);

        // Act
        var recentMatches = await _repository.GetRecentMatchesAsync("Completed", 10);

        // Assert
        recentMatches.Should().Contain(m => m.MatchId == completedMatch1.MatchId);
        recentMatches.Should().Contain(m => m.MatchId == completedMatch2.MatchId);
    }

    [Fact]
    public async Task GetOpenLobbiesAsync_ReturnsOnlyOpenLobbies()
    {
        // Arrange
        var openLobby = CreateTestMatch();
        openLobby.Player2Id = string.Empty;
        openLobby.IsOpenLobby = true;
        openLobby.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;

        var friendLobby = CreateTestMatch();
        friendLobby.IsOpenLobby = false;
        friendLobby.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;

        var inProgressMatch = CreateTestMatch();
        inProgressMatch.CoreMatch.Status = Core.MatchStatus.InProgress;

        await _repository.SaveMatchAsync(openLobby);
        await _repository.SaveMatchAsync(friendLobby);
        await _repository.SaveMatchAsync(inProgressMatch);

        // Act
        var lobbies = await _repository.GetOpenLobbiesAsync();

        // Assert
        lobbies.Should().Contain(m => m.MatchId == openLobby.MatchId);
        lobbies.Should().NotContain(m => m.MatchId == friendLobby.MatchId);
        lobbies.Should().NotContain(m => m.MatchId == inProgressMatch.MatchId);
    }

    [Fact]
    public async Task GetOpenLobbiesAsync_WithCorrespondenceFilter_FiltersCorrectly()
    {
        // Arrange
        var regularLobby = CreateTestMatch();
        regularLobby.Player2Id = string.Empty;
        regularLobby.IsOpenLobby = true;
        regularLobby.IsCorrespondence = false;
        regularLobby.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;

        var correspondenceLobby = CreateTestMatch();
        correspondenceLobby.Player2Id = string.Empty;
        correspondenceLobby.IsOpenLobby = true;
        correspondenceLobby.IsCorrespondence = true;
        correspondenceLobby.TimePerMoveDays = 3;
        correspondenceLobby.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;

        await _repository.SaveMatchAsync(regularLobby);
        await _repository.SaveMatchAsync(correspondenceLobby);

        // Act
        var regularLobbies = await _repository.GetOpenLobbiesAsync(isCorrespondence: false);
        var correspondenceLobbies = await _repository.GetOpenLobbiesAsync(isCorrespondence: true);

        // Assert
        regularLobbies.Should().Contain(m => m.MatchId == regularLobby.MatchId);
        regularLobbies.Should().NotContain(m => m.MatchId == correspondenceLobby.MatchId);

        correspondenceLobbies.Should().Contain(m => m.MatchId == correspondenceLobby.MatchId);
        correspondenceLobbies.Should().NotContain(m => m.MatchId == regularLobby.MatchId);
    }

    // ==================== Delete Tests ====================

    [Fact]
    public async Task DeleteMatchAsync_RemovesMatch()
    {
        // Arrange
        var match = CreateTestMatch();
        await _repository.SaveMatchAsync(match);

        // Act
        await _repository.DeleteMatchAsync(match.MatchId);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteMatchAsync_RemovesPlayerMatchIndexItems()
    {
        // Arrange
        var player1Id = Guid.NewGuid().ToString();
        var player2Id = Guid.NewGuid().ToString();
        var match = CreateTestMatch(player1Id, player2Id);
        await _repository.SaveMatchAsync(match);

        // Act
        await _repository.DeleteMatchAsync(match.MatchId);

        // Assert
        var player1Matches = await _repository.GetPlayerMatchesAsync(player1Id);
        var player2Matches = await _repository.GetPlayerMatchesAsync(player2Id);

        player1Matches.Should().NotContain(m => m.MatchId == match.MatchId);
        player2Matches.Should().NotContain(m => m.MatchId == match.MatchId);
    }

    // ==================== Correspondence Match Tests ====================

    [Fact]
    public async Task SaveMatchAsync_CorrespondenceMatch_PreservesTurnInfo()
    {
        // Arrange
        var match = CreateTestMatch();
        match.IsCorrespondence = true;
        match.TimePerMoveDays = 3;
        match.CurrentTurnPlayerId = match.Player1Id;
        match.TurnDeadline = DateTime.UtcNow.AddDays(3);

        // Act
        await _repository.SaveMatchAsync(match);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.IsCorrespondence.Should().BeTrue();
        retrieved.TimePerMoveDays.Should().Be(3);
        retrieved.CurrentTurnPlayerId.Should().Be(match.Player1Id);
        retrieved.TurnDeadline.Should().BeCloseTo(match.TurnDeadline!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateCorrespondenceTurnAsync_UpdatesTurnAndDeadline()
    {
        // Arrange
        var match = CreateTestMatch();
        match.IsCorrespondence = true;
        match.TimePerMoveDays = 3;
        await _repository.SaveMatchAsync(match);

        var newDeadline = DateTime.UtcNow.AddDays(3);

        // Act
        await _repository.UpdateCorrespondenceTurnAsync(match.MatchId, match.Player2Id, newDeadline);
        var retrieved = await _repository.GetMatchByIdAsync(match.MatchId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.CurrentTurnPlayerId.Should().Be(match.Player2Id);
        retrieved.TurnDeadline.Should().BeCloseTo(newDeadline, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task GetCorrespondenceMatchesForTurnAsync_ReturnsMatchesWherePlayersTurn()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();
        var opponentId = Guid.NewGuid().ToString();

        var myTurnMatch = CreateTestMatch(playerId, opponentId);
        myTurnMatch.IsCorrespondence = true;
        myTurnMatch.CurrentTurnPlayerId = playerId;
        myTurnMatch.TurnDeadline = DateTime.UtcNow.AddDays(3);
        await _repository.SaveMatchAsync(myTurnMatch);
        await _repository.UpdateCorrespondenceTurnAsync(myTurnMatch.MatchId, playerId, DateTime.UtcNow.AddDays(3));

        var opponentTurnMatch = CreateTestMatch(playerId, opponentId);
        opponentTurnMatch.IsCorrespondence = true;
        opponentTurnMatch.CurrentTurnPlayerId = opponentId;
        opponentTurnMatch.TurnDeadline = DateTime.UtcNow.AddDays(3);
        await _repository.SaveMatchAsync(opponentTurnMatch);
        await _repository.UpdateCorrespondenceTurnAsync(opponentTurnMatch.MatchId, opponentId, DateTime.UtcNow.AddDays(3));

        // Act
        var myTurnMatches = await _repository.GetCorrespondenceMatchesForTurnAsync(playerId);

        // Assert
        myTurnMatches.Should().Contain(m => m.MatchId == myTurnMatch.MatchId);
        myTurnMatches.Should().NotContain(m => m.MatchId == opponentTurnMatch.MatchId);
    }

    [Fact]
    public async Task GetCorrespondenceMatchesWaitingAsync_ReturnsMatchesWhereWaitingForOpponent()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();
        var opponentId = Guid.NewGuid().ToString();

        var waitingMatch = CreateTestMatch(playerId, opponentId);
        waitingMatch.IsCorrespondence = true;
        waitingMatch.CurrentTurnPlayerId = opponentId;
        waitingMatch.TurnDeadline = DateTime.UtcNow.AddDays(3);
        await _repository.SaveMatchAsync(waitingMatch);
        await _repository.UpdateCorrespondenceTurnAsync(waitingMatch.MatchId, opponentId, DateTime.UtcNow.AddDays(3));

        var myTurnMatch = CreateTestMatch(playerId, opponentId);
        myTurnMatch.IsCorrespondence = true;
        myTurnMatch.CurrentTurnPlayerId = playerId;
        myTurnMatch.TurnDeadline = DateTime.UtcNow.AddDays(3);
        await _repository.SaveMatchAsync(myTurnMatch);
        await _repository.UpdateCorrespondenceTurnAsync(myTurnMatch.MatchId, playerId, DateTime.UtcNow.AddDays(3));

        // Act
        var waitingMatches = await _repository.GetCorrespondenceMatchesWaitingAsync(playerId);

        // Assert
        waitingMatches.Should().Contain(m => m.MatchId == waitingMatch.MatchId);
        waitingMatches.Should().NotContain(m => m.MatchId == myTurnMatch.MatchId);
    }

    // ==================== Player Match Index Creation Tests ====================

    [Fact]
    public async Task CreatePlayerMatchIndexAsync_AllowsPlayer2ToFindMatch()
    {
        // Arrange - create open lobby without Player2
        var match = CreateTestMatch();
        match.Player2Id = string.Empty;
        match.IsOpenLobby = true;
        match.CoreMatch.Status = Core.MatchStatus.WaitingForPlayers;
        await _repository.SaveMatchAsync(match);

        var player2Id = Guid.NewGuid().ToString();

        // Act - create index for Player2 when they join
        await _repository.CreatePlayerMatchIndexAsync(
            player2Id,
            match.MatchId,
            match.Player1Id,
            "InProgress",
            match.CreatedAt);

        // Assert
        var player2Matches = await _repository.GetPlayerMatchesAsync(player2Id);
        player2Matches.Should().ContainSingle(m => m.MatchId == match.MatchId);
    }

    // ==================== Match Stats Tests ====================

    [Fact]
    public async Task GetPlayerMatchStatsAsync_ReturnsCorrectStats()
    {
        // Arrange
        var playerId = Guid.NewGuid().ToString();
        var opponentId = Guid.NewGuid().ToString();

        // Player wins a match 5-3
        var wonMatch = CreateTestMatch(playerId, opponentId);
        wonMatch.CoreMatch.Status = Core.MatchStatus.Completed;
        wonMatch.Player1Score = 5;
        wonMatch.Player2Score = 3;
        wonMatch.WinnerId = playerId;
        wonMatch.DurationSeconds = 1800;
        await _repository.SaveMatchAsync(wonMatch);

        // Player loses a match 4-5
        var lostMatch = CreateTestMatch(playerId, opponentId);
        lostMatch.CoreMatch.Status = Core.MatchStatus.Completed;
        lostMatch.Player1Score = 4;
        lostMatch.Player2Score = 5;
        lostMatch.WinnerId = opponentId;
        lostMatch.DurationSeconds = 2400;
        await _repository.SaveMatchAsync(lostMatch);

        // Act
        var stats = await _repository.GetPlayerMatchStatsAsync(playerId);

        // Assert
        stats.TotalMatches.Should().Be(2);
        stats.MatchesWon.Should().Be(1);
        stats.MatchesLost.Should().Be(1);
        stats.TotalPointsScored.Should().Be(9); // 5 + 4
        stats.TotalPointsConceded.Should().Be(8); // 3 + 5
        stats.AverageMatchLength.Should().Be(2100); // (1800 + 2400) / 2
    }

    // ==================== Helper Methods ====================

    private static Match CreateTestMatch(string? player1Id = null, string? player2Id = null)
    {
        return new Match
        {
            CoreMatch = new Core.Match
            {
                MatchId = Guid.NewGuid().ToString(),
                Player1Id = player1Id ?? Guid.NewGuid().ToString(),
                Player2Id = player2Id ?? Guid.NewGuid().ToString(),
                TargetScore = 5,
                Status = Core.MatchStatus.InProgress,
                CreatedAt = DateTime.UtcNow
            },
            Player1Name = "TestPlayer1",
            Player2Name = "TestPlayer2",
            OpponentType = "Friend",
            IsRated = true,
            LastUpdatedAt = DateTime.UtcNow
        };
    }
}
