using Amazon.CDK;
using Amazon.CDK.AWS.IAM;
using Constructs;

namespace Infra;

public class GitHubOidcConstruct : Construct
{
    public GitHubOidcConstruct(Construct scope, string id, string githubUsername, string githubRepo)
        : base(scope, id)
    {
        // Create GitHub OIDC Provider
        var oidcProvider = new OpenIdConnectProvider(this, "GitHubOidcProvider", new OpenIdConnectProviderProps
        {
            Url = "https://token.actions.githubusercontent.com",
            ClientIds = new[] { "sts.amazonaws.com" },
            Thumbprints = new[] { "6938fd4d98bab03faadb97b34396831e3780aea1" }
        });

        // Create IAM role for GitHub Actions
        var deployRole = new Role(this, "GitHubActionsDeployRole", new RoleProps
        {
            RoleName = "GitHubActionsDeployRole",
            Description = "Role for GitHub Actions to deploy Backgammon application",
            AssumedBy = new FederatedPrincipal(
                oidcProvider.OpenIdConnectProviderArn,
                new System.Collections.Generic.Dictionary<string, object>
                {
                    {
                        "StringEquals", new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "token.actions.githubusercontent.com:aud", "sts.amazonaws.com" }
                        }
                    },
                    {
                        "StringLike", new System.Collections.Generic.Dictionary<string, string>
                        {
                            { "token.actions.githubusercontent.com:sub", $"repo:{githubUsername}/{githubRepo}:*" }
                        }
                    }
                },
                "sts:AssumeRoleWithWebIdentity"),
            MaxSessionDuration = Duration.Hours(1)
        });

        // Attach managed policies
        deployRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryPowerUser"));
        deployRole.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMReadOnlyAccess"));

        // Add inline policy for EC2 describe
        deployRole.AddToPolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Actions = new[] { "ec2:DescribeInstances" },
            Resources = new[] { "*" }
        }));

        // Assign to property
        DeployRole = deployRole;

        // Outputs
        new CfnOutput(this, "GitHubActionsRoleArn", new CfnOutputProps
        {
            Value = DeployRole.RoleArn,
            Description = "ARN of the GitHub Actions deploy role",
            ExportName = "GitHubActionsDeployRoleArn"
        });

        new CfnOutput(this, "OidcProviderArn", new CfnOutputProps
        {
            Value = oidcProvider.OpenIdConnectProviderArn,
            Description = "ARN of the GitHub OIDC provider",
            ExportName = "GitHubOidcProviderArn"
        });
    }

    public IRole DeployRole { get; }
}
