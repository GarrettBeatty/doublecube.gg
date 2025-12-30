using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.IAM;
using Constructs;
using Infra;

namespace Backgammon.Infrastructure;

public class BackgammonStack : Stack
{
    public BackgammonStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Extract environment from stack name (e.g., BackgammonStack-dev -> dev)
        var environment = id.Split('-').Last();

        // Create DynamoDB table
        var dynamoDbTable = new DynamoDbConstruct(this, "DynamoDbTable", environment);

        // Create ECR repositories
        var ecr = new EcrConstruct(this, "EcrRepositories", environment);

        // Create SSM parameters for secrets and config
        var ssmParams = new SsmParameterConstruct(this, "SsmParameters", environment, dynamoDbTable.Table.TableName);

        // Create GitHub OIDC provider and IAM role for GitHub Actions
        var githubOidc = new GitHubOidcConstruct(this, "GitHubOidc", "GarrettBeatty", "Backgammon");

        // Grant GitHub Actions role access to ECR repositories
        ecr.ServerRepository.GrantPullPush(githubOidc.DeployRole);
        ecr.WebClientRepository.GrantPullPush(githubOidc.DeployRole);

        // Import default VPC (free, no cost)
        var vpc = Vpc.FromLookup(this, "DefaultVpc", new VpcLookupOptions { IsDefault = true });

        // Create Security Group
        var securityGroup = new SecurityGroup(this, "BackgammonSecurityGroup", new SecurityGroupProps
        {
            Vpc = vpc,
            Description = "Security group for Backgammon EC2 instance",
            AllowAllOutbound = true
        });

        // Allow HTTP (80), HTTPS (443), and SSH (22) inbound
        securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(80), "Allow HTTP traffic");
        securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(443), "Allow HTTPS traffic");
        securityGroup.AddIngressRule(Peer.AnyIpv4(), Port.Tcp(22), "Allow SSH for deployment");

        // Create IAM role for EC2 instance
        var ec2Role = new Role(this, "EC2InstanceRole", new RoleProps
        {
            AssumedBy = new ServicePrincipal("ec2.amazonaws.com"),
            Description = "IAM role for Backgammon EC2 instance",
            ManagedPolicies = new[]
            {
                ManagedPolicy.FromAwsManagedPolicyName("CloudWatchAgentServerPolicy"),
                ManagedPolicy.FromAwsManagedPolicyName("AmazonSSMManagedInstanceCore")
            }
        });

        // Grant DynamoDB read/write access
        dynamoDbTable.Table.GrantReadWriteData(ec2Role);

        // Grant ECR pull access to EC2 instance
        ecr.ServerRepository.GrantPull(ec2Role);
        ecr.WebClientRepository.GrantPull(ec2Role);

        // Grant ECR login permission (needed for docker login)
        ec2Role.AddManagedPolicy(ManagedPolicy.FromAwsManagedPolicyName("AmazonEC2ContainerRegistryReadOnly"));

        // Grant SSM parameter read access
        ssmParams.JwtSecretParameter.GrantRead(ec2Role);
        ssmParams.TableNameParameter.GrantRead(ec2Role);

        // EC2 User Data script - Install Docker and Docker Compose
        var userData = UserData.ForLinux();
        userData.AddCommands(
            "#!/bin/bash",
            "set -e",
            "",
            "# Update system",
            "yum update -y",
            "",
            "# Install Docker",
            "yum install -y docker",
            "systemctl start docker",
            "systemctl enable docker",
            "",
            "# Install Docker Compose",
            "curl -L https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m) -o /usr/local/bin/docker-compose",
            "chmod +x /usr/local/bin/docker-compose",
            "",
            "# Add ec2-user to docker group",
            "usermod -a -G docker ec2-user",
            "",
            "# Install AWS CLI v2 (if not already present)",
            "if ! command -v aws &> /dev/null; then",
            "  curl https://awscli.amazonaws.com/awscli-exe-linux-aarch64.zip -o awscliv2.zip",
            "  unzip awscliv2.zip",
            "  ./aws/install",
            "  rm -rf aws awscliv2.zip",
            "fi",
            "",
            "# Create deployment directory",
            "mkdir -p /home/ec2-user/backgammon",
            "chown ec2-user:ec2-user /home/ec2-user/backgammon",
            "",
            "echo 'Docker and Docker Compose installation complete'"
        );

        // Create EC2 instance (t4g.nano - ARM64, 512MB RAM, 2 vCPU)
        var instance = new Instance_(this, "BackgammonInstance", new InstanceProps
        {
            InstanceType = InstanceType.Of(InstanceClass.BURSTABLE4_GRAVITON, InstanceSize.NANO),
            MachineImage = MachineImage.LatestAmazonLinux2023(new AmazonLinux2023ImageSsmParameterProps
            {
                CpuType = AmazonLinuxCpuType.ARM_64
            }),
            Vpc = vpc,
            VpcSubnets = new SubnetSelection { SubnetType = SubnetType.PUBLIC },
            SecurityGroup = securityGroup,
            Role = ec2Role,
            UserData = userData,
            // KeyPair will be set up manually via EC2 Instance Connect or AWS Systems Manager Session Manager
            BlockDevices = new[]
            {
                new BlockDevice
                {
                    DeviceName = "/dev/xvda",
                    Volume = BlockDeviceVolume.Ebs(8, new EbsDeviceProps
                    {
                        VolumeType = EbsDeviceVolumeType.GP3,
                        DeleteOnTermination = true,
                        Encrypted = true
                    })
                }
            }
        });

        // Tag the instance
        Amazon.CDK.Tags.Of(instance).Add("Name", $"Backgammon-{environment}");
        Amazon.CDK.Tags.Of(instance).Add("Environment", environment);
        Amazon.CDK.Tags.Of(instance).Add("Application", "Backgammon");
        Amazon.CDK.Tags.Of(instance).Add("ManagedBy", "CDK");

        // Create Elastic IP
        var eip = new CfnEIP(this, "ElasticIP", new CfnEIPProps
        {
            Domain = "vpc",
            Tags = new[]
            {
                new CfnTag { Key = "Name", Value = $"Backgammon-{environment}-EIP" },
                new CfnTag { Key = "Environment", Value = environment }
            }
        });

        // Associate Elastic IP with EC2 instance
        new CfnEIPAssociation(this, "EIPAssociation", new CfnEIPAssociationProps
        {
            InstanceId = instance.InstanceId,
            AllocationId = eip.AttrAllocationId
        });

        // Outputs
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

        new CfnOutput(this, "PublicIP", new CfnOutputProps
        {
            Value = eip.Ref,
            Description = "Elastic IP address for EC2 instance",
            ExportName = $"Backgammon-{environment}-PublicIP"
        });

        new CfnOutput(this, "InstanceId", new CfnOutputProps
        {
            Value = instance.InstanceId,
            Description = "EC2 instance ID",
            ExportName = $"Backgammon-{environment}-InstanceId"
        });
    }
}
