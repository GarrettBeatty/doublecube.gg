using Amazon.CDK;
using Constructs;

namespace Backgammon.Infrastructure;

public class BackgammonStack : Stack
{
    public BackgammonStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Extract environment from stack name (e.g., BackgammonStack-dev -> dev)
        var environment = id.Split('-').Last();

        // Create DynamoDB table
        var dynamoDbTable = new DynamoDbConstruct(this, "DynamoDbTable", environment);

        // Output the table name and ARN for reference
        new CfnOutput(this, "TableName", new CfnOutputProps
        {
            Value = dynamoDbTable.Table.TableName,
            Description = "DynamoDB table name",
            ExportName = $"Backgammon-{environment}-TableName"
        });

        new CfnOutput(this, "TableArn", new CfnOutputProps
        {
            Value = dynamoDbTable.Table.TableArn,
            Description = "DynamoDB table ARN",
            ExportName = $"Backgammon-{environment}-TableArn"
        });
    }
}
