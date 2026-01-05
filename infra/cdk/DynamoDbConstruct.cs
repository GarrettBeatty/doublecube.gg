using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Backgammon.Infrastructure;

public class DynamoDbConstruct : Construct
{
    public ITable Table { get; private set; }

    public DynamoDbConstruct(Construct scope, string id, string environment)
        : base(scope, id)
    {
        var tableName = $"backgammon-{environment}";

        // Create the main table with single-table design
        var table = new Table(this, "BackgammonTable", new TableProps
        {
            TableName = tableName,

            // Partition key and sort key
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "PK",
                Type = AttributeType.STRING
            },
            SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "SK",
                Type = AttributeType.STRING
            },

            // Use on-demand billing for simplicity and cost-effectiveness
            BillingMode = BillingMode.PAY_PER_REQUEST,

            // Enable point-in-time recovery for data protection
            PointInTimeRecovery = true,

            // Encryption with AWS managed keys
            Encryption = TableEncryption.AWS_MANAGED,

            // Deletion protection for production environments
            DeletionProtection = environment == "prod",

            // Enable TTL for potential future use (game invites, expired sessions)
            TimeToLiveAttribute = "ttl",

            // Removal policy - retain for prod, destroy for dev
            RemovalPolicy = environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY
        });

        // Add Global Secondary Indexes
        // GSI1: Username Lookup Index
        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI1",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI1PK",
                Type = AttributeType.STRING
            },
            SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI1SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // GSI2: Email Lookup Index
        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI2",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI2PK",
                Type = AttributeType.STRING
            },
            SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI2SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // GSI3: Game Status Index
        table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI3",
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI3PK",
                Type = AttributeType.STRING
            },
            SortKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "GSI3SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // Assign to the ITable property
        Table = table;

        // Tag the table
        Amazon.CDK.Tags.Of(Table).Add("Project", "Backgammon");
        Amazon.CDK.Tags.Of(Table).Add("Environment", environment);
        Amazon.CDK.Tags.Of(Table).Add("ManagedBy", "CDK");
    }
}
