using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using Testcontainers.DynamoDb;

namespace Backgammon.IntegrationTests.Fixtures;

/// <summary>
/// Provides a DynamoDB Local container for integration tests.
/// Creates a unique table per test class to allow parallel execution.
/// </summary>
public class DynamoDbFixture : IAsyncLifetime
{
    private DynamoDbContainer? _container;

    /// <summary>
    /// Gets the DynamoDB client connected to the test container.
    /// </summary>
    public IAmazonDynamoDB Client { get; private set; } = null!;

    /// <summary>
    /// Gets the unique table name for this test fixture.
    /// </summary>
    public string TableName { get; } = $"backgammon-test-{Guid.NewGuid():N}";

    /// <summary>
    /// Gets the connection string for the DynamoDB container.
    /// </summary>
    public string ConnectionString => _container?.GetConnectionString() ?? throw new InvalidOperationException("Container not started");

    public async Task InitializeAsync()
    {
        // Start DynamoDB Local container
        _container = new DynamoDbBuilder()
            .WithImage("amazon/dynamodb-local:latest")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8000))
            .Build();

        await _container.StartAsync();

        // Create client
        var config = new AmazonDynamoDBConfig
        {
            ServiceURL = _container.GetConnectionString()
        };

        Client = new AmazonDynamoDBClient(new BasicAWSCredentials("test", "test"), config);

        // Create table with same schema as production
        await CreateTableAsync();
    }

    public async Task DisposeAsync()
    {
        // Delete table first
        try
        {
            await Client.DeleteTableAsync(TableName);
        }
        catch
        {
            // Ignore errors during cleanup
        }

        // Dispose client
        Client?.Dispose();

        // Stop container
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates the DynamoDB table with the same schema as production.
    /// Mirrors DynamoDbInitializer.CreateTableAsync().
    /// </summary>
    private async Task CreateTableAsync()
    {
        var request = new CreateTableRequest
        {
            TableName = TableName,

            // Primary key
            KeySchema =
            [
                new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
            ],

            // Attribute definitions (only for keys and GSI keys)
            AttributeDefinitions =
            [
                new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI1PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI1SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI2PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI2SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI3PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI3SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI4PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI4SK", AttributeType = ScalarAttributeType.S }
            ],

            // On-demand billing (pay per request)
            BillingMode = BillingMode.PAY_PER_REQUEST,

            // Global Secondary Indexes
            GlobalSecondaryIndexes =
            [
                // GSI1: Username Lookup Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI1",
                    KeySchema =
                    [
                        new KeySchemaElement { AttributeName = "GSI1PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI1SK", KeyType = KeyType.RANGE }
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                },

                // GSI2: Email Lookup Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI2",
                    KeySchema =
                    [
                        new KeySchemaElement { AttributeName = "GSI2PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI2SK", KeyType = KeyType.RANGE }
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                },

                // GSI3: Game/Match Status Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI3",
                    KeySchema =
                    [
                        new KeySchemaElement { AttributeName = "GSI3PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI3SK", KeyType = KeyType.RANGE }
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                },

                // GSI4: Correspondence "My Turn" Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI4",
                    KeySchema =
                    [
                        new KeySchemaElement { AttributeName = "GSI4PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI4SK", KeyType = KeyType.RANGE }
                    ],
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                }
            ]
        };

        await Client.CreateTableAsync(request);

        // Wait for table to become active
        await WaitForTableActiveAsync();
    }

    private async Task WaitForTableActiveAsync()
    {
        var maxAttempts = 30;
        var attemptCount = 0;

        while (attemptCount < maxAttempts)
        {
            var response = await Client.DescribeTableAsync(TableName);

            if (response.Table.TableStatus == TableStatus.ACTIVE)
            {
                return;
            }

            await Task.Delay(500);
            attemptCount++;
        }

        throw new TimeoutException($"Table '{TableName}' did not become active within the expected time");
    }
}
