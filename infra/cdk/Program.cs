using Amazon.CDK;

namespace Backgammon.Infrastructure;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        var app = new App();

        // Get environment from context or use default
        var environment = app.Node.TryGetContext("environment")?.ToString() ?? "dev";

        new BackgammonStack(app, $"BackgammonStack-{environment}", new StackProps
        {
            Env = new Amazon.CDK.Environment
            {
                Account = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT"),
                Region = System.Environment.GetEnvironmentVariable("CDK_DEFAULT_REGION") ?? "us-east-1"
            },
            Description = $"Backgammon application infrastructure ({environment})",
            Tags = new Dictionary<string, string>
            {
                { "Project", "Backgammon" },
                { "Environment", environment }
            }
        });

        app.Synth();
    }
}
