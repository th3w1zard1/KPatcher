# Andastra Web Integration

This directory contains the complete web integration for running Andastra/KOTOR game engine in the browser using WebAssembly.

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker (optional, for containerized deployment)
- Modern browser (Chrome 86+, Edge 86+, Safari 15.2+)

### Local Development

1. **Build the WASM project:**
   ```bash
   cd Game.Wasm
   dotnet publish -c Release -r browser-wasm
   ```

2. **Run the API server:**
   ```bash
   cd Api
   dotnet run
   ```

3. **Open browser:**
   Navigate to `http://localhost:5000`

### Using Build Script

For the complete build pipeline (WASM + encryption):

```bash
cd /path/to/HoloPatcher.NET
./scripts/build-wasm.sh
```

### Docker Deployment

Simple deployment:
```bash
docker-compose -f docker-compose.web.yml up --build
```

With Nginx reverse proxy:
```bash
docker-compose -f docker-compose.web.yml --profile with-nginx up --build
```

## Project Structure

```
Web/
├── Api/                    # ASP.NET backend
│   ├── Program.cs         # Minimal API with endpoints
│   └── *.csproj
│
├── Crypto/                # Encryption/decryption utilities
│   ├── WasmEncryption.cs  # AES-256-GCM implementation
│   ├── EphemeralKeyGenerator.cs
│   └── *.csproj
│
└── Frontend/              # Browser client
    ├── html/
    │   └── index.html
    ├── css/
    │   └── styles.css
    └── js/
        ├── crypto.js      # Web Crypto API wrapper
        ├── filesystem.js  # File System Access API
        ├── wasm-loader.js # WASM loading/decryption
        └── app.js         # Main application
```

## API Endpoints

### POST /api/runtime/key
Issues ephemeral decryption key (5-minute validity)

**Response:**
```json
{
  "key": "base64-encoded-key",
  "validFor": "5 minutes",
  "timestamp": 1702840123
}
```

### GET /api/runtime/wasm
Serves encrypted WASM binary

**Response:** Binary data (application/wasm-encrypted)

### GET /api/version
Returns version and capability information

**Response:**
```json
{
  "api": { "version": "1.0.0" },
  "wasm": { "version": "1.0.0", "engine": "Stride 4.2" },
  "security": { "encryption": "AES-256-GCM" }
}
```

## Security

### Encryption
- **Algorithm:** AES-256-GCM with authenticated encryption
- **Key Size:** 32 bytes (256 bits)
- **Nonce:** 12 bytes random per encryption
- **Tag:** 16 bytes authentication

### Key Derivation
- **Method:** HMAC-SHA256
- **Factors:** Client IP + User-Agent + Time Window
- **Window:** 5 minutes (with 1-window tolerance for clock skew)

### Best Practices
1. Store master secret in secure vault (Azure Key Vault, AWS Secrets Manager)
2. Use HTTPS in production with valid SSL certificate
3. Implement rate limiting (10 req/s for keys, 1 req/min for WASM)
4. Enable monitoring and alerting
5. Rotate master secret periodically

## Browser Support

| Feature | Chrome | Edge | Safari | Firefox |
|---------|--------|------|--------|---------|
| WebAssembly | ✅ 57+ | ✅ 16+ | ✅ 11+ | ✅ 52+ |
| File System Access | ✅ 86+ | ✅ 86+ | ✅ 15.2+ | ⚠️ Polyfill |
| Web Crypto | ✅ 37+ | ✅ 79+ | ✅ 11+ | ✅ 34+ |

## Configuration

### Environment Variables

```bash
# Required in production
MASTER_SECRET=<base64-encoded-32-byte-secret>

# Optional
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:5000
```

### Generating Master Secret

```bash
# Generate a new master secret
openssl rand -base64 32

# Or using .NET
dotnet script -e "Console.WriteLine(Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)))"
```

## Documentation

For comprehensive documentation, see:
- [WEB_INTEGRATION.md](../../../docs/WEB_INTEGRATION.md) - Complete architecture and deployment guide
- Inline code comments in all source files
- Frontend JavaScript documentation

## Troubleshooting

### "WASM file not found"
- Ensure build completed: `dotnet publish src/Andastra/Game.Wasm/Andastra.Game.Wasm.csproj`
- Check `Api/wwwroot/wasm/` directory exists
- Verify file permissions

### "Decryption failed"
- Check time synchronization between client and server
- Verify master secret is consistent
- Key has 5-minute validity (10 minutes with tolerance)

### "Browser not supported"
- Update to latest browser version
- Chrome/Edge 86+ or Safari 15.2+ required
- Firefox requires File System Access polyfill

## License

Business Source License 1.1 (BSL-1.1)
See LICENSE file in repository root.

## Support

- GitHub Issues: https://github.com/th3w1zard1/HoloPatcher.NET/issues
- Documentation: [WEB_INTEGRATION.md](../../../docs/WEB_INTEGRATION.md)
