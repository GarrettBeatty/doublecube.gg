using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Backgammon.Server.Models;
using Backgammon.Server.Services.DynamoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ServerMatch = Backgammon.Server.Models.Match;

namespace Backgammon.Tests;

/// <summary>
/// Integration tests for DynamoDbMatchRepository that test actual DynamoDB operations.
/// These tests verify the DynamoDB update expressions work correctly.
/// </summary>
/// <remarks>
/// Note: These tests require DynamoDB Local to be running on localhost:8000.
/// Run with: docker run -p 8000:8000 amazon/dynamodb-local
/// Or use Aspire which starts DynamoDB Local automatically.
/// </remarks>
public class DynamoDbMatchRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly string _testTableName = $"backgammon-test-{Guid.NewGuid():N}";
    private IAmazonDynamoDB? _dynamoDbClient;
    private DynamoDbMatchRepository? _repository;
    private bool _dynamoDbAvailable;

    public async Task InitializeAsync()
    {
        try
        {
            // Create DynamoDB client pointing to local instance
            var config = new AmazonDynamoDBConfig
            {
                ServiceURL = "http://localhost:8000",
                AuthenticationRegion = "us-east-1"
            };
            _dynamoDbClient = new AmazonDynamoDBClient(config);

            // Test if DynamoDB Local is available
            await _dynamoDbClient.ListTablesAsync();
            _dynamoDbAvailable = true;

            // Create test table
            await CreateTestTableAsync();

            // Create repository
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DynamoDb:TableName"] = _testTableName
                })
                .Build();

            var logger = new Mock<ILogger<DynamoDbMatchRepository>>().Object;
            _repository = new DynamoDbMatchRepository(_dynamoDbClient, configuration, logger);
        }
        catch (Exception)
        {
            _dynamoDbAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_dynamoDbAvailable && _dynamoDbClient != null)
        {
            try
            {
                // Delete test table
                await _dynamoDbClient.DeleteTableAsync(_testTableName);
            }
            catch
            {
                // Ignore cleanup errors
            }

            _dynamoDbClient.Dispose();
        }

        await Task.CompletedTask;
    }

    [Fact]
    public async Task AddGameToMatchAsync_AddsFirstGame_WhenGameIdsDoesNotExist()
    {
        // Skip if DynamoDB Local is not available
        if (!_dynamoDbAvailable)
        {
            // This is expected in CI/CD environments without DynamoDB Local
            return;
        }

        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var gameId = Guid.NewGuid().ToString();

        // Create a match without gameIds field
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "player2",
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            TargetScore = 3,
            Player1Score = 0,
            Player2Score = 0,
            Status = "InProgress",
            OpponentType = "Friend",
            GameIds = new List<string>() // Empty list initially
        };

        Assert.NotNull(_repository);
        await _repository.SaveMatchAsync(match);

        // Act - Add first game
        await _repository.AddGameToMatchAsync(matchId, gameId);

        // Assert - Retrieve and verify
        var retrievedMatch = await _repository.GetMatchByIdAsync(matchId);
        Assert.NotNull(retrievedMatch);
        Assert.Single(retrievedMatch.GameIds);
        Assert.Equal(gameId, retrievedMatch.GameIds[0]);
        Assert.Equal(gameId, retrievedMatch.CurrentGameId);
    }

    [Fact]
    public async Task AddGameToMatchAsync_AppendsToExistingList_WhenGameIdsExists()
    {
        // Skip if DynamoDB Local is not available
        if (!_dynamoDbAvailable)
        {
            return;
        }

        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var game1Id = Guid.NewGuid().ToString();
        var game2Id = Guid.NewGuid().ToString();
        var game3Id = Guid.NewGuid().ToString();

        // Create a match with existing games
        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "player2",
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            TargetScore = 5,
            Player1Score = 2,
            Player2Score = 1,
            Status = "InProgress",
            OpponentType = "Friend",
            GameIds = new List<string> { game1Id, game2Id },
            CurrentGameId = game2Id
        };

        Assert.NotNull(_repository);
        await _repository.SaveMatchAsync(match);

        // Act - Add third game
        await _repository.AddGameToMatchAsync(matchId, game3Id);

        // Assert - Retrieve and verify
        var retrievedMatch = await _repository.GetMatchByIdAsync(matchId);
        Assert.NotNull(retrievedMatch);
        Assert.Equal(3, retrievedMatch.GameIds.Count);
        Assert.Equal(game1Id, retrievedMatch.GameIds[0]);
        Assert.Equal(game2Id, retrievedMatch.GameIds[1]);
        Assert.Equal(game3Id, retrievedMatch.GameIds[2]);
        Assert.Equal(game3Id, retrievedMatch.CurrentGameId);
    }

    [Fact]
    public async Task AddGameToMatchAsync_UpdatesLastUpdatedAt()
    {
        // Skip if DynamoDB Local is not available
        if (!_dynamoDbAvailable)
        {
            return;
        }

        // Arrange
        var matchId = Guid.NewGuid().ToString();
        var gameId = Guid.NewGuid().ToString();

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "player2",
            Player1Name = "Player 1",
            Player2Name = "Player 2",
            TargetScore = 3,
            Player1Score = 0,
            Player2Score = 0,
            Status = "InProgress",
            OpponentType = "Friend",
            GameIds = new List<string>(),
            LastUpdatedAt = DateTime.UtcNow.AddHours(-1) // Set to 1 hour ago
        };

        Assert.NotNull(_repository);
        await _repository.SaveMatchAsync(match);
        var originalUpdateTime = match.LastUpdatedAt;

        // Act
        await Task.Delay(100); // Ensure time difference
        await _repository.AddGameToMatchAsync(matchId, gameId);

        // Assert
        var retrievedMatch = await _repository.GetMatchByIdAsync(matchId);
        Assert.NotNull(retrievedMatch);
        Assert.True(
            retrievedMatch.LastUpdatedAt > originalUpdateTime,
            "LastUpdatedAt should be updated to a more recent time");
    }

    [Fact]
    public async Task AddGameToMatchAsync_WorksCorrectlyInMatchContinuationFlow()
    {
        // Skip if DynamoDB Local is not available
        if (!_dynamoDbAvailable)
        {
            return;
        }

        // This test simulates the full match continuation flow
        // to verify the fix for the "Continue to Next Game" bug

        // Arrange - Create a match to 3 points
        var matchId = Guid.NewGuid().ToString();
        var game1Id = Guid.NewGuid().ToString();

        var match = new ServerMatch
        {
            MatchId = matchId,
            Player1Id = "player1",
            Player2Id = "ai_opponent",
            Player1Name = "Human Player",
            Player2Name = "Greedy Bot",
            TargetScore = 3,
            Player1Score = 0,
            Player2Score = 0,
            Status = "InProgress",
            OpponentType = "AI",
            GameIds = new List<string> { game1Id },
            CurrentGameId = game1Id
        };

        Assert.NotNull(_repository);
        await _repository.SaveMatchAsync(match);

        // Simulate first game completing with a Gammon win (2 points)
        match.Player1Score = 2;
        await _repository.UpdateMatchAsync(match);

        // Act - Continue to next game (this is where the bug was occurring)
        var game2Id = Guid.NewGuid().ToString();
        await _repository.AddGameToMatchAsync(matchId, game2Id);

        // Assert - Verify match state after continuing
        var retrievedMatch = await _repository.GetMatchByIdAsync(matchId);
        Assert.NotNull(retrievedMatch);
        Assert.Equal(2, retrievedMatch.GameIds.Count);
        Assert.Equal(game1Id, retrievedMatch.GameIds[0]);
        Assert.Equal(game2Id, retrievedMatch.GameIds[1]);
        Assert.Equal(game2Id, retrievedMatch.CurrentGameId);
        Assert.Equal(2, retrievedMatch.Player1Score);
        Assert.Equal(0, retrievedMatch.Player2Score);
        Assert.Equal("InProgress", retrievedMatch.Status);

        // Simulate second game and continue to third (Crawford game)
        match.Player2Score = 2;
        match.IsCrawfordGame = true;
        await _repository.UpdateMatchAsync(match);

        var game3Id = Guid.NewGuid().ToString();
        await _repository.AddGameToMatchAsync(matchId, game3Id);

        // Assert - Verify Crawford game state
        retrievedMatch = await _repository.GetMatchByIdAsync(matchId);
        Assert.NotNull(retrievedMatch);
        Assert.Equal(3, retrievedMatch.GameIds.Count);
        Assert.Equal(game3Id, retrievedMatch.CurrentGameId);
        Assert.True(retrievedMatch.IsCrawfordGame);
    }

    private async Task CreateTestTableAsync()
    {
        if (_dynamoDbClient == null)
        {
            return;
        }

        var request = new CreateTableRequest
        {
            TableName = _testTableName,
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S }
            },
            BillingMode = BillingMode.PAY_PER_REQUEST
        };

        await _dynamoDbClient.CreateTableAsync(request);

        // Wait for table to be active
        var describeRequest = new DescribeTableRequest { TableName = _testTableName };
        TableDescription? tableDescription = null;
        do
        {
            await Task.Delay(100);
            var response = await _dynamoDbClient.DescribeTableAsync(describeRequest);
            tableDescription = response.Table;
        }
        while (tableDescription.TableStatus != TableStatus.ACTIVE);
    }
}
