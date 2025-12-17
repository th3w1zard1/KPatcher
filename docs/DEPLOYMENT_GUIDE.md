# Andastra Web Deployment Guide

This guide provides step-by-step instructions for deploying the Andastra web integration in various environments.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Local Development Setup](#local-development-setup)
3. [Production Deployment](#production-deployment)
4. [Docker Deployment](#docker-deployment)
5. [Cloud Deployment](#cloud-deployment)
6. [Security Configuration](#security-configuration)
7. [Monitoring and Maintenance](#monitoring-and-maintenance)

## Prerequisites

### Required Software

- **.NET 9.0 SDK** or later
  - Download: https://dotnet.microsoft.com/download
  - Verify: `dotnet --version`

- **Docker** (for containerized deployment)
  - Download: https://docs.docker.com/get-docker/
  - Verify: `docker --version`

- **Docker Compose** (usually included with Docker Desktop)
  - Verify: `docker-compose --version`

### Required Workloads

For WASM builds:
```bash
dotnet workload install wasm-tools
```

### Browser Requirements

- Chrome/Edge 86+ (recommended)
- Safari 15.2+ (macOS 12.3+ / iOS 15.2+)
- Firefox with File System Access polyfill

## Local Development Setup

### 1. Clone Repository

```bash
git clone https://github.com/th3w1zard1/HoloPatcher.NET.git
cd HoloPatcher.NET
```

### 2. Build Components

#### Build Crypto Library

```bash
cd src/Andastra/Web/Crypto
dotnet build
```

#### Build API Server

```bash
cd ../Api
dotnet build
```

### 3. Generate Master Secret

**Important:** Generate a secure master secret for encryption key derivation:

```bash
# Using OpenSSL (Linux/Mac)
openssl rand -base64 32

# Using PowerShell (Windows)
[Convert]::ToBase64String((New-Object byte[] 32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Using .NET
dotnet script -e "Console.WriteLine(Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)))"
```

Save this value securely - you'll need it for configuration.

### 4. Configure API

Edit `src/Andastra/Web/Api/appsettings.Development.json`:

```json
{
  "Security": {
    "MasterSecret": "YOUR_BASE64_SECRET_HERE"
  }
}
```

**Never commit this file to source control!**

### 5. Run API Server

```bash
cd src/Andastra/Web/Api
dotnet run
```

The API will be available at: `http://localhost:5000`

### 6. Test API Endpoints

```bash
# Test version endpoint
curl http://localhost:5000/api/version

# Request ephemeral key
curl -X POST http://localhost:5000/api/runtime/key

# Expected response:
# {
#   "key": "base64-encoded-key",
#   "validFor": "5 minutes",
#   "timestamp": 1702840123
# }
```

## Production Deployment

### 1. Build for Production

#### Build WASM with AOT

```bash
./scripts/build-wasm.sh
```

This script:
- Compiles WASM with AOT
- Strips IL and metadata
- Encrypts the WASM binary
- Generates encryption keys

Output locations:
- WASM: `build/wasm/`
- Encrypted: `build/encrypted/`

#### Copy Encrypted WASM to API

```bash
mkdir -p src/Andastra/Web/Api/wwwroot/wasm
cp build/encrypted/Andastra.Game.Wasm.wasm.encrypted src/Andastra/Web/Api/wwwroot/wasm/
```

### 2. Configure Production Settings

Create `src/Andastra/Web/Api/appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Security": {
    "MasterSecret": "",  // Set via environment variable
    "KeyValidityMinutes": 5
  },
  "AllowedHosts": "your-domain.com"
}
```

### 3. Set Environment Variables

**Linux/Mac:**
```bash
export ASPNETCORE_ENVIRONMENT=Production
export Security__MasterSecret="YOUR_BASE64_SECRET"
export ASPNETCORE_URLS="http://+:5000"
```

**Windows (PowerShell):**
```powershell
$env:ASPNETCORE_ENVIRONMENT="Production"
$env:Security__MasterSecret="YOUR_BASE64_SECRET"
$env:ASPNETCORE_URLS="http://+:5000"
```

### 4. Build and Publish

```bash
cd src/Andastra/Web/Api
dotnet publish -c Release -o publish
```

### 5. Run Production Build

```bash
cd publish
./Andastra.Web.Api
```

Or with systemd (Linux):

```bash
sudo nano /etc/systemd/system/andastra-web.service
```

```ini
[Unit]
Description=Andastra Web API
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/opt/andastra/publish
ExecStart=/usr/bin/dotnet /opt/andastra/publish/Andastra.Web.Api.dll
Restart=always
RestartSec=10
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="Security__MasterSecret=YOUR_SECRET"

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable andastra-web
sudo systemctl start andastra-web
sudo systemctl status andastra-web
```

## Docker Deployment

### Simple Deployment

1. **Build and run:**

```bash
docker-compose -f docker-compose.web.yml up --build
```

2. **Access:**

Navigate to `http://localhost:5000`

### Production Deployment with Nginx

1. **Create `.env` file:**

```bash
cat > .env << EOF
MASTER_SECRET=YOUR_BASE64_SECRET_HERE
ASPNETCORE_ENVIRONMENT=Production
EOF
```

2. **Configure Nginx (optional SSL):**

Edit `nginx.conf` to uncomment HTTPS section and update:
- Domain name
- SSL certificate paths

3. **Deploy:**

```bash
docker-compose -f docker-compose.web.yml --profile with-nginx up -d
```

4. **View logs:**

```bash
docker-compose -f docker-compose.web.yml logs -f
```

### Docker Commands

```bash
# Stop services
docker-compose -f docker-compose.web.yml down

# Restart services
docker-compose -f docker-compose.web.yml restart

# View logs
docker-compose -f docker-compose.web.yml logs andastra-web

# Execute shell in container
docker-compose -f docker-compose.web.yml exec andastra-web /bin/bash
```

## Cloud Deployment

### Azure App Service

1. **Create App Service:**

```bash
az webapp create --name andastra-web \
  --resource-group myResourceGroup \
  --plan myAppServicePlan \
  --runtime "DOTNET:9.0"
```

2. **Configure Settings:**

```bash
az webapp config appsettings set --name andastra-web \
  --resource-group myResourceGroup \
  --settings Security__MasterSecret="YOUR_SECRET"
```

3. **Deploy:**

```bash
cd src/Andastra/Web/Api
dotnet publish -c Release
cd bin/Release/net9.0/publish
az webapp deploy --name andastra-web \
  --resource-group myResourceGroup \
  --src-path .
```

### AWS Elastic Beanstalk

1. **Install EB CLI:**

```bash
pip install awsebcli
```

2. **Initialize:**

```bash
eb init -p "64bit Amazon Linux 2023 v3.1.0 running .NET 9" andastra-web
```

3. **Set environment variables:**

```bash
eb setenv Security__MasterSecret="YOUR_SECRET"
```

4. **Deploy:**

```bash
eb create andastra-web-env
eb deploy
```

### Google Cloud Run

1. **Build container:**

```bash
gcloud builds submit --tag gcr.io/PROJECT_ID/andastra-web
```

2. **Deploy:**

```bash
gcloud run deploy andastra-web \
  --image gcr.io/PROJECT_ID/andastra-web \
  --platform managed \
  --set-env-vars Security__MasterSecret="YOUR_SECRET"
```

## Security Configuration

### SSL/TLS Setup

#### Using Let's Encrypt (Nginx)

1. **Install Certbot:**

```bash
sudo apt-get install certbot python3-certbot-nginx
```

2. **Obtain certificate:**

```bash
sudo certbot --nginx -d your-domain.com
```

3. **Auto-renewal:**

```bash
sudo systemctl enable certbot.timer
```

#### Using Custom Certificate

1. **Place certificates:**

```bash
mkdir -p ssl
cp your-cert.pem ssl/cert.pem
cp your-key.pem ssl/key.pem
```

2. **Update `nginx.conf`:**

Uncomment HTTPS section and verify paths.

### Secrets Management

#### Azure Key Vault

```bash
# Store secret
az keyvault secret set --vault-name myKeyVault \
  --name AndastraMasterSecret \
  --value "YOUR_SECRET"

# Configure app to use Key Vault
az webapp config appsettings set --name andastra-web \
  --settings Security__MasterSecret="@Microsoft.KeyVault(SecretUri=https://myKeyVault.vault.azure.net/secrets/AndastraMasterSecret/)"
```

#### AWS Secrets Manager

```bash
# Create secret
aws secretsmanager create-secret \
  --name andastra/master-secret \
  --secret-string "YOUR_SECRET"

# Reference in application
export Security__MasterSecret=$(aws secretsmanager get-secret-value \
  --secret-id andastra/master-secret \
  --query SecretString \
  --output text)
```

#### HashiCorp Vault

```bash
# Write secret
vault kv put secret/andastra master-secret="YOUR_SECRET"

# Read secret
export Security__MasterSecret=$(vault kv get -field=master-secret secret/andastra)
```

### Rate Limiting

Configure in `nginx.conf`:

```nginx
# API key requests: 10/second
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;

# WASM downloads: 1/minute
limit_req_zone $binary_remote_addr zone=wasm_limit:10m rate=1r/m;
```

## Monitoring and Maintenance

### Health Checks

API provides health check endpoint:

```bash
curl http://localhost:5000/api/version
```

### Logging

#### View logs (Docker):

```bash
docker-compose -f docker-compose.web.yml logs -f andastra-web
```

#### View logs (systemd):

```bash
sudo journalctl -u andastra-web -f
```

### Metrics

Monitor these key metrics:
- **API Response Time**: Should be <100ms
- **Key Generation Rate**: Watch for spikes
- **WASM Download Rate**: Should be low (cached)
- **Error Rate**: Should be <1%

### Backup and Recovery

#### Backup master secret:

```bash
# Export to secure location
echo $Security__MasterSecret > /secure/location/master-secret.backup
chmod 600 /secure/location/master-secret.backup
```

#### Rotate master secret:

1. Generate new secret
2. Update configuration
3. Restart services
4. Monitor for errors

### Updates

#### Update application:

```bash
git pull
./scripts/build-wasm.sh
docker-compose -f docker-compose.web.yml up --build -d
```

#### Update dependencies:

```bash
dotnet outdated
dotnet update
```

## Troubleshooting

### Common Issues

**"WASM build failed"**
- Install wasm-tools: `dotnet workload install wasm-tools`
- Check .NET SDK version: `dotnet --version` (should be 9.0+)

**"Decryption failed"**
- Verify master secret is consistent across builds
- Check client/server time synchronization
- Key validity is 5 minutes

**"Browser not supported"**
- Update browser to latest version
- Chrome/Edge 86+, Safari 15.2+ required
- Firefox needs File System Access polyfill

**"WASM file not found"**
- Ensure encrypted WASM is in `wwwroot/wasm/`
- Check file permissions
- Verify build completed successfully

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Support

- GitHub Issues: https://github.com/th3w1zard1/HoloPatcher.NET/issues
- Documentation: [WEB_INTEGRATION.md](WEB_INTEGRATION.md)

## License

Business Source License 1.1 (BSL-1.1)
Production use requires explicit authorization from the licensor.
