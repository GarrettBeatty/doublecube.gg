# Deployment Guide — AWS Lightsail

This guide walks through deploying the Backgammon application to an AWS Lightsail Instance running `docker-compose`. Images are pushed to GitHub Container Registry (GHCR) by the `Deploy to Lightsail` GitHub Actions workflow; the deploy step then SSHes into the Lightsail Instance and pulls the new images.

## Architecture

```
GitHub push → Actions builds 3 images (server, webclient, gnubg)
            → pushes to ghcr.io/garrettbeatty/backgammon-{name}
            → SSHes to Lightsail Instance
            → docker compose pull && up -d (6 containers)
```

The instance runs everything in containers: `postgres`, `redis`, `gnubg-service`, `server`, `webclient`, `caddy`. Persistent state (Postgres data, Redis AOF, Caddy certificates) lives on the instance's root disk.

## Prerequisites

- AWS account with Lightsail access
- A registered domain (for HTTPS via Caddy + Let's Encrypt)
- An SSH key pair for the deploy bot (separate from any personal key)

## 1. Create the Lightsail Instance

1. Open the [Lightsail console](https://lightsail.aws.amazon.com/).
2. **Create instance**:
   - Region: pick one close to your users (e.g., `us-east-1`).
   - Platform: **Linux/Unix**.
   - Blueprint: **Ubuntu 24.04 LTS** (OS Only).
   - Instance plan: **$10/mo** at minimum (2 GB RAM, 2 vCPU, 60 GB SSD). The $5 plan (512 MB) cannot run the full 6-container stack reliably.
   - Name: `backgammon-prod` (or whatever you like).
3. **Create**.

## 2. Attach a Static IP

1. In Lightsail: **Networking → Create static IP**.
2. Attach it to the instance you just created.
3. Note the IP — you'll need it for DNS and GitHub secrets.

## 3. Open the Firewall

In the instance's **Networking** tab, add these inbound rules (in addition to the default SSH on 22):

| Application | Protocol | Port |
|---|---|---|
| HTTP | TCP | 80 |
| HTTPS | TCP | 443 |

Caddy handles TLS termination on 443 and ACME challenges on 80.

## 4. First-Time Server Setup

SSH into the instance using the default Lightsail key (downloadable from the console's **Account → SSH keys** page) or the browser-based SSH session:

```bash
ssh -i ~/.ssh/LightsailDefaultKey.pem ubuntu@<STATIC_IP>
```

### Install Docker + Compose

```bash
# Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu

# Re-login for the group change to take effect
exit
# (SSH back in)

# Verify
docker --version
docker compose version   # Compose v2 ships with the Docker package
```

### Create the deploy directory

```bash
mkdir -p ~/backgammon
cd ~/backgammon
```

### Create the `.env` file with secrets

```bash
cat > ~/backgammon/.env <<'EOF'
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<generate a strong password>
JWT_SECRET=<generate a 64-char random string>
DOMAIN=yourdomain.com
TLS_EMAIL=you@yourdomain.com
EOF

chmod 600 ~/backgammon/.env
```

Generate secrets locally with `openssl rand -base64 48` and paste them in.

### Add the deploy bot's public SSH key

Generate a fresh keypair locally (no passphrase — required for non-interactive GitHub Actions deploys):

```bash
ssh-keygen -t ed25519 -f ~/.ssh/backgammon-deploy -C "backgammon-ci" -N ""
```

Append the public key to the instance's `authorized_keys`:

```bash
# On your laptop
cat ~/.ssh/backgammon-deploy.pub

# On the instance
echo "<paste public key>" >> ~/.ssh/authorized_keys
```

### Configure GHCR pull access

If your GHCR images are **public**, you can skip this. If they are **private** (the default), the instance needs a GitHub Personal Access Token with `read:packages` scope:

1. On GitHub: **Settings → Developer settings → Personal access tokens → Tokens (classic) → Generate new token** with the `read:packages` scope. Save the token.
2. On the instance:
   ```bash
   echo "<TOKEN>" | docker login ghcr.io -u garrettbeatty --password-stdin
   ```
   The credential is cached at `~/.docker/config.json` and reused by subsequent `docker compose pull` calls.

## 5. DNS

Point your domain at the Lightsail static IP:

| Record | Name | Value |
|---|---|---|
| A | `@` (or subdomain) | `<STATIC_IP>` |
| A | `www` (optional) | `<STATIC_IP>` |

DNS propagation can take a few minutes to a few hours. Caddy will automatically obtain a Let's Encrypt certificate on first request once the domain resolves.

## 6. GitHub Repository Secrets

In **Settings → Secrets and variables → Actions**, add:

| Secret | Value |
|---|---|
| `LIGHTSAIL_HOST` | Lightsail static IP (e.g., `54.83.12.45`) |
| `LIGHTSAIL_USER` | `ubuntu` (or whichever user owns `~/backgammon`) |
| `LIGHTSAIL_SSH_PRIVATE_KEY` | Contents of `~/.ssh/backgammon-deploy` (the **private** key) |

The workflow does not need any AWS credentials — GHCR auth uses `GITHUB_TOKEN`, which is injected automatically.

## 7. First Deployment

Push to `main` (or run the workflow manually from the **Actions** tab). The workflow will:

1. Build three ARM64 images and push them to `ghcr.io/garrettbeatty/backgammon-{server,webclient,gnubg}:<sha>` (and `:latest`).
2. SCP `docker-compose.prod.yml` and `Caddyfile` to the instance.
3. SSH in and run `docker compose pull && up -d`.
4. Wait for every container's health check to report `healthy`.
5. Reload Caddy.

You can also deploy manually from the instance once images exist:

```bash
cd ~/backgammon
export IMAGE_TAG=latest
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

## 8. Verify

From your laptop:

```bash
curl -I https://yourdomain.com/health
# HTTP/2 200
```

From the instance:

```bash
docker ps --format 'table {{.Names}}\t{{.Status}}'
docker compose -f docker-compose.prod.yml logs --tail 50 server
```

## Backups

Lightsail Instances support automatic snapshots (in **Snapshots** tab) — schedule a daily snapshot at minimum. The Postgres data lives in the `postgres_data` Docker volume on the instance's disk, so a Lightsail snapshot captures it. For point-in-time DB backup, run `pg_dump` from a periodic cron job and write the dump elsewhere (e.g., S3 or Backblaze B2).

## Troubleshooting

- **`docker compose pull` fails with `denied: denied`** — GHCR credentials missing or expired. Re-run `docker login ghcr.io` on the instance with a fresh PAT.
- **Caddy can't get a certificate** — DNS hasn't propagated yet, or port 80 is blocked. `dig yourdomain.com` from your laptop to confirm the A record resolves to the static IP; `docker logs backgammon-caddy` for ACME errors.
- **Server can't reach Postgres** — `docker compose logs postgres` and confirm `POSTGRES_PASSWORD` in `.env` is set. The server reads `ConnectionStrings__Postgres` from compose, which references the same variable.
- **Out of disk** — the gnubg image is large and old images accumulate. The workflow runs `docker image prune -af` after each deploy; you can also run it manually.

## Tear-down

To dismantle:

1. Lightsail console → **Instances → Stop**, then **Delete instance**.
2. Lightsail console → **Networking → Release** the static IP.
3. Remove the deploy-bot PAT from GitHub.
4. Optionally delete the GHCR packages under **Profile → Packages**.
