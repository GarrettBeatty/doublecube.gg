using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Backgammon.Server.Services.DynamoDb;

public class DynamoDbInitializer : IHostedService
{
    private readonly IAmazonDynamoDB _dynamoDbClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DynamoDbInitializer> _logger;
    private readonly string _tableName;
    private readonly bool _isLocalEnvironment;
    private readonly TaskCompletionSource<bool> _initializationTcs = new();

    /// <summary>
    /// Task that completes when DynamoDB initialization is finished.
    /// Throws if initialization fails.
    /// </summary>
    public Task InitializationComplete => _initializationTcs.Task;

    public DynamoDbInitializer(
        IAmazonDynamoDB dynamoDbClient,
        IConfiguration configuration,
        ILogger<DynamoDbInitializer> logger)
    {
        _dynamoDbClient = dynamoDbClient;
        _configuration = configuration;
        _logger = logger;
        _tableName = configuration["DynamoDb:TableName"] ?? "backgammon-local";
        // Detect local environment by checking if Aspire set the local endpoint
        _isLocalEnvironment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_ENDPOINT_URL_DYNAMODB"));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing DynamoDB...");
        _logger.LogInformation("Table name: {TableName}", _tableName);
        _logger.LogInformation("Local environment: {IsLocal}", _isLocalEnvironment);

        try
        {
            // Check if table exists
            var tableExists = await TableExistsAsync(cancellationToken);

            if (tableExists)
            {
                _logger.LogInformation("DynamoDB table '{TableName}' already exists", _tableName);
                _initializationTcs.SetResult(true);
                return;
            }

            if (!_isLocalEnvironment)
            {
                _logger.LogWarning("Table '{TableName}' does not exist in production environment. " +
                    "Table should be created via CDK. Skipping table creation.", _tableName);
                _initializationTcs.SetResult(true);
                return;
            }

            // Create table for local development
            _logger.LogInformation("Creating DynamoDB table '{TableName}'...", _tableName);
            await CreateTableAsync(cancellationToken);
            _logger.LogInformation("DynamoDB table '{TableName}' created successfully", _tableName);
            _initializationTcs.SetResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing DynamoDB");
            _initializationTcs.SetException(ex);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<bool> TableExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await _dynamoDbClient.DescribeTableAsync(_tableName, cancellationToken);
            return response.Table != null;
        }
        catch (ResourceNotFoundException)
        {
            return false;
        }
    }

    private async Task CreateTableAsync(CancellationToken cancellationToken)
    {
        var request = new CreateTableRequest
        {
            TableName = _tableName,

            // Primary key
            KeySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement { AttributeName = "PK", KeyType = KeyType.HASH },
                new KeySchemaElement { AttributeName = "SK", KeyType = KeyType.RANGE }
            },

            // Attribute definitions (only for keys and GSI keys)
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new AttributeDefinition { AttributeName = "PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI1PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI1SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI2PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI2SK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI3PK", AttributeType = ScalarAttributeType.S },
                new AttributeDefinition { AttributeName = "GSI3SK", AttributeType = ScalarAttributeType.S }
            },

            // On-demand billing (pay per request)
            BillingMode = BillingMode.PAY_PER_REQUEST,

            // Global Secondary Indexes
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                // GSI1: Username Lookup Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI1",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = "GSI1PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI1SK", KeyType = KeyType.RANGE }
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                },

                // GSI2: Email Lookup Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI2",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = "GSI2PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI2SK", KeyType = KeyType.RANGE }
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                },

                // GSI3: Game Status Index
                new GlobalSecondaryIndex
                {
                    IndexName = "GSI3",
                    KeySchema = new List<KeySchemaElement>
                    {
                        new KeySchemaElement { AttributeName = "GSI3PK", KeyType = KeyType.HASH },
                        new KeySchemaElement { AttributeName = "GSI3SK", KeyType = KeyType.RANGE }
                    },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL }
                }
            }
        };

        var response = await _dynamoDbClient.CreateTableAsync(request, cancellationToken);

        _logger.LogInformation("Table creation initiated. Status: {Status}", response.TableDescription.TableStatus);

        // Wait for table to become active
        await WaitForTableActiveAsync(cancellationToken);
    }

    private async Task WaitForTableActiveAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Waiting for table '{TableName}' to become active...", _tableName);

        var maxAttempts = 30;
        var attemptCount = 0;

        while (attemptCount < maxAttempts)
        {
            try
            {
                var response = await _dynamoDbClient.DescribeTableAsync(_tableName, cancellationToken);

                if (response.Table.TableStatus == TableStatus.ACTIVE)
                {
                    _logger.LogInformation("Table '{TableName}' is now active", _tableName);
                    return;
                }

                _logger.LogInformation("Table status: {Status}. Waiting...", response.Table.TableStatus);
                await Task.Delay(2000, cancellationToken);
                attemptCount++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking table status (attempt {Attempt}/{MaxAttempts})",
                    attemptCount + 1, maxAttempts);
                attemptCount++;
                await Task.Delay(2000, cancellationToken);
            }
        }

        throw new TimeoutException($"Table '{_tableName}' did not become active within the expected time");
    }
}
