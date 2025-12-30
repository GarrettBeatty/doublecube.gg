using Amazon.CDK;
using Amazon.CDK.AWS.ECR;
using Constructs;

namespace Infra;

public class EcrConstruct : Construct
{
    public IRepository ServerRepository { get; }
    public IRepository WebClientRepository { get; }

    public EcrConstruct(Construct scope, string id, string environment) : base(scope, id)
    {
        // Server repository
        ServerRepository = new Repository(this, "ServerRepo", new RepositoryProps
        {
            RepositoryName = $"backgammon-server-{environment}",
            RemovalPolicy = environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
            LifecycleRules = new[]
            {
                new LifecycleRule
                {
                    MaxImageCount = 5,
                    Description = "Keep last 5 images only"
                }
            },
            ImageScanOnPush = true,
            ImageTagMutability = TagMutability.MUTABLE
        });

        // WebClient repository
        WebClientRepository = new Repository(this, "WebClientRepo", new RepositoryProps
        {
            RepositoryName = $"backgammon-webclient-{environment}",
            RemovalPolicy = environment == "prod" ? RemovalPolicy.RETAIN : RemovalPolicy.DESTROY,
            LifecycleRules = new[]
            {
                new LifecycleRule
                {
                    MaxImageCount = 5,
                    Description = "Keep last 5 images only"
                }
            },
            ImageScanOnPush = true,
            ImageTagMutability = TagMutability.MUTABLE
        });

        // Outputs
        new CfnOutput(this, "ServerRepositoryUri", new CfnOutputProps
        {
            Value = ServerRepository.RepositoryUri,
            Description = "ECR repository URI for Server",
            ExportName = $"Backgammon-{environment}-ServerRepoUri"
        });

        new CfnOutput(this, "WebClientRepositoryUri", new CfnOutputProps
        {
            Value = WebClientRepository.RepositoryUri,
            Description = "ECR repository URI for WebClient",
            ExportName = $"Backgammon-{environment}-WebClientRepoUri"
        });
    }
}
