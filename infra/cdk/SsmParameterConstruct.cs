using Amazon.CDK;
using Amazon.CDK.AWS.SSM;
using Constructs;

namespace Infra;

public class SsmParameterConstruct : Construct
{
    public IStringParameter JwtSecretParameter { get; }
    public IStringParameter TableNameParameter { get; }

    public SsmParameterConstruct(Construct scope, string id, string environment, string tableName) : base(scope, id)
    {
        // JWT Secret (secure string) - placeholder value, update manually or via GitHub Actions
        // Note: CDK StringParameter creates SecureString by default, no Type parameter needed
        JwtSecretParameter = new StringParameter(this, "JwtSecret", new StringParameterProps
        {
            ParameterName = $"/backgammon/{environment}/jwt-secret",
            StringValue = "PLACEHOLDER-UPDATE-AFTER-DEPLOYMENT",
            Description = "JWT secret for token signing and validation",
            Tier = ParameterTier.STANDARD
        });

        // DynamoDB Table Name
        TableNameParameter = new StringParameter(this, "TableName", new StringParameterProps
        {
            ParameterName = $"/backgammon/{environment}/table-name",
            StringValue = tableName,
            Description = "DynamoDB table name for the Backgammon application",
            Tier = ParameterTier.STANDARD
        });

        // Outputs
        new CfnOutput(this, "JwtSecretParameterName", new CfnOutputProps
        {
            Value = JwtSecretParameter.ParameterName,
            Description = "SSM parameter name for JWT secret",
            ExportName = $"Backgammon-{environment}-JwtSecretParam"
        });

        new CfnOutput(this, "TableNameParameterName", new CfnOutputProps
        {
            Value = TableNameParameter.ParameterName,
            Description = "SSM parameter name for DynamoDB table name",
            ExportName = $"Backgammon-{environment}-TableNameParam"
        });
    }
}
