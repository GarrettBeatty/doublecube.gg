using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Constructs;

namespace Backgammon.Infrastructure;

public class DynamoDbConstruct : Construct
{
    public ITable Table { get; }

    public DynamoDbConstruct(Construct scope, string id, string environment) : base(scope, id)
    {
        var tableName = $"backgammon-{environment}";

        // Create the main table with single-table design
        Table = new Table(this, "BackgammonTable", new TableProps
        {
            TableName = tableName,

            // Partition key and sort key
            PartitionKey = new Attribute
            {
                Name = "PK",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
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

        // GSI1: Username Lookup Index
        Table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI1",
            PartitionKey = new Attribute
            {
                Name = "GSI1PK",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
            {
                Name = "GSI1SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // GSI2: Email Lookup Index
        Table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI2",
            PartitionKey = new Attribute
            {
                Name = "GSI2PK",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
            {
                Name = "GSI2SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // GSI3: Game Status Index
        Table.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI3",
            PartitionKey = new Attribute
            {
                Name = "GSI3PK",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
            {
                Name = "GSI3SK",
                Type = AttributeType.STRING
            },
            ProjectionType = ProjectionType.ALL
        });

        // Tag the table
        Tags.Of(Table).Add("Project", "Backgammon");
        Tags.Of(Table).Add("Environment", environment);
        Tags.Of(Table).Add("ManagedBy", "CDK");
    }
}
