using Amazon.CDK;
using Amazon.CDK.AWS.SSM;
using Constructs;

namespace Infra;

public class SsmParameterConstruct : Construct
{
    public SsmParameterConstruct(Construct scope, string id, string environment, string tableName)
        : base(scope, id)
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

        // Domain name for custom domain and SSL
        DomainParameter = new StringParameter(this, "Domain", new StringParameterProps
        {
            ParameterName = $"/backgammon/{environment}/domain",
            StringValue = "localhost", // Default value - will be overridden manually
            Description = "Custom domain for Backgammon deployment",
            Tier = ParameterTier.STANDARD
        });

        // TLS email for Let's Encrypt certificate notifications
        TlsEmailParameter = new StringParameter(this, "TlsEmail", new StringParameterProps
        {
            ParameterName = $"/backgammon/{environment}/tls-email",
            StringValue = "admin@example.com", // Default value - will be overridden manually
            Description = "Email for Let's Encrypt certificate notifications",
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

        new CfnOutput(this, "DomainParameterName", new CfnOutputProps
        {
            Value = DomainParameter.ParameterName,
            Description = "SSM parameter name for domain",
            ExportName = $"Backgammon-{environment}-DomainParam"
        });

        new CfnOutput(this, "TlsEmailParameterName", new CfnOutputProps
        {
            Value = TlsEmailParameter.ParameterName,
            Description = "SSM parameter name for TLS email",
            ExportName = $"Backgammon-{environment}-TlsEmailParam"
        });
    }

    public IStringParameter JwtSecretParameter { get; }

    public IStringParameter TableNameParameter { get; }

    public IStringParameter DomainParameter { get; }

    public IStringParameter TlsEmailParameter { get; }
}
