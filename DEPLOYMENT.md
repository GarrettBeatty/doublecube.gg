# AWS Deployment Guide

This guide provides step-by-step instructions for deploying the Backgammon application to AWS.

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Initial CDK Deployment](#initial-cdk-deployment)
3. [SSH Key Setup](#ssh-key-setup)
4. [Get the Elastic IP Address](#get-the-elastic-ip-address)
5. [Configure GitHub Repository Secrets](#configure-github-repository-secrets)
6. [Update SSM Parameters](#update-ssm-parameters)
7. [DNS Configuration](#dns-configuration)
8. [Trigger First Deployment](#trigger-first-deployment)
9. [Verify Deployment](#verify-deployment)
10. [Troubleshooting](#troubleshooting)

## Prerequisites

Before deploying, ensure you have:

- **AWS Account** with appropriate permissions
- **AWS CLI** installed and configured with credentials
  ```bash
  aws configure
  # Enter your AWS Access Key ID, Secret Access Key, and region (us-east-1)
  ```
- **AWS CDK CLI** installed
  ```bash
  npm install -g aws-cdk
  ```
- **Git** and access to the GitHub repository
- **Domain name** (optional, for custom domain with HTTPS)

## Initial CDK Deployment

1. Navigate to the CDK directory:
   ```bash
   cd infra/cdk
   ```

2. Bootstrap CDK (only needed once per AWS account/region):
   ```bash
   cdk bootstrap
   ```

3. Deploy the infrastructure:
   ```bash
   cdk deploy BackgammonStack-dev
   ```

4. **Important**: Note the stack outputs displayed at the end of deployment:
   - `PublicIP` - Elastic IP address for your EC2 instance
   - `InstanceId` - EC2 instance ID (needed for SSH setup)
   - `TableName` - DynamoDB table name
   - `ServerRepositoryUri` - ECR repository for server images
   - `WebClientRepositoryUri` - ECR repository for web client images

## SSH Key Setup

The EC2 instance is deployed **without a KeyPair** configured. You must manually set up SSH access using one of the following methods:

### Option A: Generate SSH Key Locally (Recommended)

This is the recommended approach as it gives you full control over the SSH key.

1. **Generate a new SSH key pair** (without passphrase for GitHub Actions):
   ```bash
   ssh-keygen -t rsa -b 4096 -f ~/.ssh/backgammon-deploy-key -C "backgammon-deployment"
   ```
   When prompted for a passphrase, **press Enter twice** to create a key without a passphrase (required for GitHub Actions automation).

2. **Get the public key content**:
   ```bash
   cat ~/.ssh/backgammon-deploy-key.pub
   ```
   Copy the entire output (starts with `ssh-rsa ...`).

3. **Get your EC2 instance ID** (if not saved from CDK output):
   ```bash
   aws cloudformation describe-stacks \
     --stack-name BackgammonStack-dev \
     --query 'Stacks[0].Outputs[?OutputKey==`InstanceId`].OutputValue' \
     --output text
   ```

4. **Connect to the EC2 instance using AWS Systems Manager**:
   ```bash
   aws ssm start-session --target <INSTANCE_ID>
   ```
   Replace `<INSTANCE_ID>` with the instance ID from step 3.

5. **On the EC2 instance**, add your public key to authorized_keys:
   ```bash
   sudo su - ec2-user
   mkdir -p ~/.ssh
   chmod 700 ~/.ssh
   echo "PASTE_YOUR_PUBLIC_KEY_HERE" >> ~/.ssh/authorized_keys
   chmod 600 ~/.ssh/authorized_keys
   exit
   exit
   ```
   Replace `PASTE_YOUR_PUBLIC_KEY_HERE` with the public key from step 2.

6. **Test the SSH connection** (get the Elastic IP from next section):
   ```bash
   ssh -i ~/.ssh/backgammon-deploy-key ec2-user@<ELASTIC_IP>
   ```

### Option B: Use EC2 Instance Connect (Browser-Based)

If you prefer using the AWS Console:

1. Go to the [EC2 Console](https://console.aws.amazon.com/ec2/)
2. Navigate to **Instances** and select your Backgammon instance
3. Click **Connect** → **EC2 Instance Connect** → **Connect**
4. In the browser-based terminal, run:
   ```bash
   mkdir -p ~/.ssh
   chmod 700 ~/.ssh
   nano ~/.ssh/authorized_keys
   ```
5. Paste your public key (from `cat ~/.ssh/backgammon-deploy-key.pub`), save (Ctrl+O, Enter, Ctrl+X)
6. Set correct permissions:
   ```bash
   chmod 600 ~/.ssh/authorized_keys
   ```

### Option C: Modify CDK to Include KeyPair (For Future Deployments)

If you want the CDK to manage the SSH key from the start:

1. **Create a key pair in AWS**:
   ```bash
   aws ec2 create-key-pair --key-name backgammon-deploy-key --query 'KeyMaterial' --output text > ~/.ssh/backgammon-deploy-key.pem
   chmod 600 ~/.ssh/backgammon-deploy-key.pem
   ```

2. **Edit `infra/cdk/BackgammonStack.cs`** at line 113, add the `KeyName` property:
   ```csharp
   var instance = new Instance_(this, "BackgammonInstance", new InstanceProps
   {
       InstanceType = InstanceType.Of(InstanceClass.BURSTABLE4_GRAVITON, InstanceSize.NANO),
       // ... other properties ...
       KeyName = "backgammon-deploy-key",  // Add this line
       // ... rest of properties ...
   });
   ```

3. **Redeploy**:
   ```bash
   cd infra/cdk
   cdk deploy BackgammonStack-dev
   ```

## Get the Elastic IP Address

You need the Elastic IP address for GitHub Actions configuration and accessing your application.

### Method 1: From CDK Deploy Output
Look for the `PublicIP` value in the stack outputs displayed after running `cdk deploy`.

### Method 2: AWS CLI
```bash
aws cloudformation describe-stacks \
  --stack-name BackgammonStack-dev \
  --query 'Stacks[0].Outputs[?OutputKey==`PublicIP`].OutputValue' \
  --output text
```

### Method 3: AWS Console
1. Go to [EC2 → Elastic IPs](https://console.aws.amazon.com/ec2/#Addresses)
2. Look for the IP with the tag `Name: Backgammon-dev-EIP`

## Configure GitHub Repository Secrets

GitHub Actions uses two secrets to deploy to your EC2 instance. These must be configured before the first deployment.

1. **Navigate to your repository secrets**:
   Go to: https://github.com/GarrettBeatty/Backgammon/settings/secrets/actions

2. **Add Secret: EC2_HOST**
   - Click **New repository secret**
   - Name: `EC2_HOST`
   - Secret: Enter the Elastic IP address from the previous section (e.g., `54.123.45.67`)
   - Click **Add secret**

3. **Add Secret: EC2_SSH_PRIVATE_KEY**
   - Click **New repository secret**
   - Name: `EC2_SSH_PRIVATE_KEY`
   - Secret: The **entire contents** of your private key file

   **macOS/Linux - Copy private key to clipboard**:
   ```bash
   # macOS
   cat ~/.ssh/backgammon-deploy-key | pbcopy

   # Linux
   cat ~/.ssh/backgammon-deploy-key  # Copy output manually
   ```

   The key should include:
   ```
   -----BEGIN OPENSSH PRIVATE KEY-----
   ... (multiple lines of key data) ...
   -----END OPENSSH PRIVATE KEY-----
   ```

   Paste the complete key into the secret value field and click **Add secret**.

## Update SSM Parameters

The CDK deployment creates AWS Systems Manager (SSM) parameters with placeholder values. You must update these before deploying your application.

### Generate and Set JWT Secret

```bash
# Generate a secure random secret (32 bytes, base64 encoded)
JWT_SECRET=$(openssl rand -base64 32)

# Store in SSM Parameter Store
aws ssm put-parameter \
  --name /backgammon/dev/jwt-secret \
  --value "$JWT_SECRET" \
  --type SecureString \
  --overwrite
```

### Set Your Domain Names

The application supports multiple domains simultaneously. Both `backgammon.beatty.codes` and `doublecube.gg` are configured in the Caddyfile. Set both domains in a comma-separated list for backend CORS configuration:

```bash
aws ssm put-parameter \
  --name /backgammon/dev/domain \
  --value "backgammon.beatty.codes,doublecube.gg" \
  --overwrite
```

This configures the backend to accept SignalR connections and API requests from both domains. You can add additional domains by separating them with commas.

### Set Let's Encrypt Email

```bash
aws ssm put-parameter \
  --name /backgammon/dev/tls-email \
  --value "garrett@beatty.codes" \
  --overwrite
```

Replace with your email address (used for Let's Encrypt certificate notifications).

### Verify Parameters

```bash
# Check table name (set automatically by CDK)
aws ssm get-parameter --name /backgammon/dev/table-name --query Parameter.Value --output text

# Check domains (should return comma-separated list)
aws ssm get-parameter --name /backgammon/dev/domain --query Parameter.Value --output text
# Expected: backgammon.beatty.codes,doublecube.gg

# Check TLS email
aws ssm get-parameter --name /backgammon/dev/tls-email --query Parameter.Value --output text

# Check JWT secret (requires --with-decryption flag)
aws ssm get-parameter --name /backgammon/dev/jwt-secret --with-decryption --query Parameter.Value --output text
```

## DNS Configuration

The application is configured to serve on two domains. You must configure DNS for both to use HTTPS (via Let's Encrypt).

### Create DNS A Records for Both Domains

In your DNS provider(s), create A records for both domains pointing to your Elastic IP:

#### Domain 1: backgammon.beatty.codes (Subdomain)

In your DNS provider for `beatty.codes`:
- **Host/Name**: `backgammon`
- **Type**: `A`
- **Value/Target**: Your Elastic IP address (e.g., `54.123.45.67`)
- **TTL**: `300` (5 minutes) or your preference

#### Domain 2: doublecube.gg (Apex Domain)

In your DNS provider for `doublecube.gg`:
- **Host/Name**: `@` (apex/root domain)
- **Type**: `A`
- **Value/Target**: Your Elastic IP address (same as above)
- **TTL**: `300` (5 minutes) or your preference

**Important**: Both domains must point to the same Elastic IP address.

### Verify DNS Propagation for Both Domains

```bash
# Wait a few minutes after creating the records, then check both:
dig backgammon.beatty.codes
dig doublecube.gg

# Or use nslookup
nslookup backgammon.beatty.codes
nslookup doublecube.gg
```

Both outputs should show your Elastic IP address.

## Trigger First Deployment

Once GitHub secrets are configured and SSM parameters are set, you can trigger your first deployment.

### Option 1: Push to Main Branch

Any push to the `main` branch automatically triggers deployment:

```bash
git add .
git commit -m "Configure deployment"
git push origin main
```

### Option 2: Manual Workflow Trigger

You can also manually trigger a deployment:

1. Go to: https://github.com/GarrettBeatty/Backgammon/actions
2. Select the **Deploy to AWS** workflow
3. Click **Run workflow** → **Run workflow**

### Monitor Deployment

- Watch the GitHub Actions tab for real-time progress
- Deployment typically takes **5-10 minutes** (building ARM64 Docker images takes most of the time)
- The workflow will:
  1. Build server and webclient Docker images for ARM64 architecture
  2. Push images to ECR
  3. SSH to EC2 instance
  4. Copy docker-compose.prod.yml and Caddyfile
  5. Pull new images
  6. Restart Docker containers
  7. Verify health endpoints

## Verify Deployment

### Check Services on EC2

SSH into your EC2 instance:

```bash
ssh -i ~/.ssh/backgammon-deploy-key ec2-user@<ELASTIC_IP>
```

Check running containers:

```bash
docker ps
```

You should see three containers:
- `backgammon-caddy-1` (reverse proxy with HTTPS)
- `backgammon-server-1` (backend API)
- `backgammon-webclient-1` (frontend)

View logs:

```bash
cd ~/backgammon
docker-compose -f docker-compose.prod.yml logs

# Or view logs for a specific service
docker-compose -f docker-compose.prod.yml logs server
docker-compose -f docker-compose.prod.yml logs webclient
docker-compose -f docker-compose.prod.yml logs caddy
```

### Test Health Endpoints

From your local machine:

```bash
# Test via IP address (HTTP)
curl http://<ELASTIC_IP>/health

# Test via both domains (HTTPS) - after DNS propagates
curl https://backgammon.beatty.codes/health
curl https://doublecube.gg/health
```

All should return a success response.

### Access the Application

The application is accessible from multiple URLs:
- **HTTP**: `http://<ELASTIC_IP>`
- **HTTPS**: `https://backgammon.beatty.codes` (Caddy automatically generates Let's Encrypt certificates)
- **HTTPS**: `https://doublecube.gg`

## Troubleshooting

### SSH Connection Fails

**Problem**: Cannot SSH to EC2 instance

**Solutions**:
- Verify your security group allows port 22 from your IP:
  ```bash
  aws ec2 describe-security-groups --group-ids <SECURITY_GROUP_ID>
  ```
- Check that your public key is in `~/.ssh/authorized_keys` on the EC2 instance
- Verify key permissions locally:
  ```bash
  chmod 600 ~/.ssh/backgammon-deploy-key
  ```
- Try using Systems Manager Session Manager instead:
  ```bash
  aws ssm start-session --target <INSTANCE_ID>
  ```

### GitHub Actions Deployment Fails

**Problem**: Deployment workflow fails at SSH step

**Solutions**:
- Verify both secrets are set correctly in repository settings
- Ensure SSH key has **no passphrase** (GitHub Actions requires this)
- Check the GitHub Actions logs for specific errors
- Verify the `EC2_HOST` value is correct (Elastic IP, not instance ID)
- Confirm the private key in the secret matches the public key on EC2

### HTTPS Certificate Fails to Generate

**Problem**: Let's Encrypt certificate generation fails

**Solutions**:
- Verify both DNS records are pointing to the correct Elastic IP:
  ```bash
  dig backgammon.beatty.codes
  dig doublecube.gg
  ```
- Ensure `TLS_EMAIL` SSM parameter has a valid email
- Wait 5-10 minutes after DNS changes (propagation time)
- Check Caddy logs for certificate errors:
  ```bash
  docker logs backgammon-caddy-1
  ```
- Ensure ports 80 and 443 are open in the security group
- Verify both domains in SSM parameter match your DNS configuration and Caddyfile
- Note: Caddy obtains separate certificates for each domain - both must have valid DNS records

### Application Not Responding

**Problem**: Application doesn't respond or shows errors

**Solutions**:
- Check DynamoDB table exists:
  ```bash
  aws dynamodb describe-table --table-name backgammon-dev
  ```
- Verify all SSM parameters are set correctly (see [Update SSM Parameters](#update-ssm-parameters))
- Check Docker container logs:
  ```bash
  ssh -i ~/.ssh/backgammon-deploy-key ec2-user@<ELASTIC_IP>
  docker-compose -f ~/backgammon/docker-compose.prod.yml logs
  ```
- Verify environment variables are loaded correctly:
  ```bash
  docker exec backgammon-server-1 env | grep -E "(DYNAMODB|JWT|DOMAIN)"
  ```
- Check EC2 instance has IAM permissions for DynamoDB and SSM
- Restart services:
  ```bash
  cd ~/backgammon
  docker-compose -f docker-compose.prod.yml restart
  ```

### Containers Keep Restarting

**Problem**: Docker containers are in restart loop

**Solutions**:
- Check container logs for errors:
  ```bash
  docker logs backgammon-server-1
  docker logs backgammon-webclient-1
  ```
- Verify the correct architecture images are being used (ARM64 for t4g.nano):
  ```bash
  docker inspect backgammon-server-1 | grep Architecture
  ```
- Check resource usage (t4g.nano has 512MB RAM):
  ```bash
  docker stats
  free -h
  ```
- Ensure DynamoDB table is accessible from EC2

### Need to Update Configuration

**Update SSM parameters** (e.g., to add a new domain):
```bash
# Add a new domain to the list
aws ssm put-parameter --name /backgammon/dev/domain --value "backgammon.beatty.codes,doublecube.gg,newdomain.com" --overwrite
```

**Important**: If you add a new domain, you must also:
1. Update the Caddyfile to include the new domain
2. Create DNS A record pointing to the Elastic IP
3. Redeploy the application

**Restart services to pick up changes**:
```bash
ssh -i ~/.ssh/backgammon-deploy-key ec2-user@<ELASTIC_IP>
cd ~/backgammon
docker-compose -f docker-compose.prod.yml down
docker-compose -f docker-compose.prod.yml up -d
```

## Cost Optimization

Your deployment uses cost-optimized resources:

- **EC2 t4g.nano**: ~$3/month (ARM Graviton2, burstable performance)
- **DynamoDB**: Pay-per-request pricing (minimal cost for low traffic)
- **ECR**: First 500 MB free, then $0.10/GB/month
- **Data transfer**: Minimal within same region
- **Elastic IP**: Free when attached to running instance

**Estimated total**: ~$5-10/month for low-traffic usage

## Ongoing Maintenance

### Deploying Code Changes

Simply push to the main branch:
```bash
git push origin main
```

GitHub Actions will automatically build and deploy the changes.

### Viewing Logs

```bash
ssh -i ~/.ssh/backgammon-deploy-key ec2-user@<ELASTIC_IP>
cd ~/backgammon
docker-compose -f docker-compose.prod.yml logs -f --tail=100
```

### Updating Infrastructure

Modify CDK code, then:
```bash
cd infra/cdk
cdk diff  # Preview changes
cdk deploy BackgammonStack-dev
```

### Backup and Recovery

DynamoDB has point-in-time recovery enabled. To restore:
```bash
aws dynamodb restore-table-to-point-in-time \
  --source-table-name backgammon-dev \
  --target-table-name backgammon-dev-restored \
  --restore-date-time <TIMESTAMP>
```

---

**Need Help?** Check the troubleshooting section or review the GitHub Actions logs at https://github.com/GarrettBeatty/Backgammon/actions
