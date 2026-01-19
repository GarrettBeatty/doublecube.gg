---
sidebar_position: 2
---

# AWS Deployment

Deploy DoubleCube.gg to AWS using CDK infrastructure as code.

## Prerequisites

- AWS Account with appropriate permissions
- AWS CLI configured: `aws configure`
- AWS CDK CLI: `npm install -g aws-cdk`
- Docker (for building container images)

## Architecture

The AWS deployment includes:
- **EC2 Instance** - Runs the application containers
- **DynamoDB** - Game and user data storage
- **ECR** - Container image registry
- **Elastic IP** - Static public IP address
- **S3** - Static asset storage (optional)

## Initial Deployment

### 1. Bootstrap CDK

First-time only per AWS account/region:

```bash
cd infra/cdk
cdk bootstrap
```

### 2. Deploy Infrastructure

```bash
cdk deploy BackgammonStack-dev
```

Note the outputs:
- `PublicIP` - Your server's IP address
- `InstanceId` - EC2 instance ID
- `TableName` - DynamoDB table name
- `ServerRepositoryUri` - ECR repository for server
- `WebClientRepositoryUri` - ECR repository for frontend

### 3. SSH Key Setup

The EC2 instance is deployed without a KeyPair. Set up SSH access:

```bash
# Generate SSH key
ssh-keygen -t rsa -b 4096 -f ~/.ssh/backgammon-deploy-key

# Connect via SSM
aws ssm start-session --target <INSTANCE_ID>

# Add public key to EC2
sudo su - ec2-user
mkdir -p ~/.ssh
chmod 700 ~/.ssh
echo "<YOUR_PUBLIC_KEY>" >> ~/.ssh/authorized_keys
chmod 600 ~/.ssh/authorized_keys
```

### 4. Configure GitHub Secrets

Add these secrets to your GitHub repository:

| Secret | Description |
|--------|-------------|
| `AWS_ACCESS_KEY_ID` | AWS credentials |
| `AWS_SECRET_ACCESS_KEY` | AWS credentials |
| `DEPLOY_SSH_PRIVATE_KEY` | Private key from step 3 |
| `DEPLOY_HOST` | Elastic IP address |
| `JWT_SECRET` | Production JWT signing key |

### 5. Update SSM Parameters

```bash
aws ssm put-parameter \
  --name "/backgammon/dev/jwt-secret" \
  --value "<your-production-jwt-secret>" \
  --type SecureString \
  --overwrite
```

## DNS Configuration

Point your domain to the Elastic IP:

1. Create an A record: `api.doublecube.gg` → `<ELASTIC_IP>`
2. Create an A record: `doublecube.gg` → `<ELASTIC_IP>`

## Continuous Deployment

Pushes to `main` trigger GitHub Actions:

1. Build Docker images
2. Push to ECR
3. SSH to EC2
4. Pull and restart containers

## Container Configuration

On EC2, containers run via Docker Compose:

```yaml
version: '3.8'
services:
  server:
    image: ${ECR_SERVER_IMAGE}
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - DynamoDb__TableName=${TABLE_NAME}
      - Jwt__Secret=${JWT_SECRET}

  webclient:
    image: ${ECR_WEBCLIENT_IMAGE}
    ports:
      - "80:80"
      - "443:443"
```

## HTTPS with Caddy

Caddy handles TLS certificates automatically:

```
doublecube.gg {
    reverse_proxy webclient:80
}

api.doublecube.gg {
    reverse_proxy server:5000
}
```

## Monitoring

### View Logs

```bash
ssh ec2-user@<ELASTIC_IP>
docker logs backgammon-server -f
```

### Health Check

```bash
curl https://api.doublecube.gg/health
```

### DynamoDB Metrics

View in AWS Console → DynamoDB → Tables → Metrics tab.

## Scaling Considerations

For higher traffic:
- Enable Redis for SignalR backplane (multi-server support)
- Use Application Load Balancer with multiple EC2 instances
- Consider ECS Fargate for container orchestration

## Troubleshooting

### Container Won't Start

```bash
docker logs backgammon-server
```

### DynamoDB Access Denied

Check IAM role attached to EC2 has DynamoDB permissions.

### SSL Certificate Issues

Caddy logs show certificate acquisition:

```bash
docker logs caddy
```
