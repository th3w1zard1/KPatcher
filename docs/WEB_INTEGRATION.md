# Andastra Web Integration Documentation

## Overview

This document describes the production-grade web integration for the Andastra Stride/.NET game engine, enabling full client-side execution in modern web browsers using WebAssembly (WASM).

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Browser (Client)                      │
│  ┌─────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │  JS Loader  │  │   WASM AOT   │  │  Virtual FS      │   │
│  │  (crypto)   │─▶│  .NET Runtime│─▶│  (game files)    │   │
│  └─────────────┘  └──────────────┘  └──────────────────┘   │
│         │                │                     │             │
│         ▼                ▼                     ▼             │
│  ┌──────────────────────────────────────────────────────┐   │
│  │          WebGL/WebGPU Rendering (Stride)             │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                            │
                            │ HTTPS
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   Server (Docker Container)                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ASP.NET Minimal API                      │   │
│  │  • POST /api/runtime/key  (ephemeral keys)           │   │
│  │  • GET /api/runtime/wasm  (encrypted WASM)           │   │
│  │  • GET /api/version       (version info)             │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │        Static File Hosting (HTML/CSS/JS)             │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## Core Requirements

### ✅ Implemented

1. **Full Client-Side Execution**
   - All game logic runs in the browser
   - No server-side game processing
   - Server only hosts static files and API

2. **Source Code Protection**
   - AOT compilation to WebAssembly
   - No IL or reflection metadata shipped
   - Optional pre-AOT obfuscation support
   - Encrypted WASM at rest

3. **Client-Side Game Data**
   - File System Access API integration
   - Local folder selection for game files
   - Validation of chitin.key and .bif files
   - No asset uploads to server

4. **Docker Deployment**
   - Single container setup
   - ASP.NET backend + static frontend
   - Scalable as static hosting
   - Optional Nginx reverse proxy

5. **.NET WebAssembly AOT**
   - Ahead-of-Time compilation
   - WASM binary only (no JIT)
   - Full IL stripping
   - Metadata trimming

## Security Architecture

### WASM Encryption

The WASM binary is encrypted at rest and decrypted only in browser memory:

```
Build Time:
  C# Source → AOT Compile → WASM Binary → Encrypt → WASM.encrypted

Runtime:
  1. Browser requests ephemeral key from server
  2. Server derives key from (IP + User-Agent + Time Window)
  3. Browser downloads encrypted WASM
  4. Browser decrypts WASM in memory using key
  5. Browser instantiates .NET runtime
  6. Encrypted WASM and key are wiped from memory
```

### Key Features

- **AES-256-GCM**: Authenticated encryption for WASM
- **Ephemeral Keys**: Session-scoped, time-variant (5-minute validity)
- **Key Derivation**: HMAC-SHA256 based on client identity
- **In-Memory Decryption**: WASM never touches disk unencrypted
- **Memory Wiping**: Secure cleanup of sensitive data

## Project Structure

```
src/Andastra/
├── Game.Wasm/                      # WebAssembly game project
│   ├── Andastra.Game.Wasm.csproj  # AOT-enabled project file
│   └── Program.cs                  # WASM entry point with JS exports
│
├── Web/
│   ├── Api/                        # ASP.NET backend
│   │   ├── Andastra.Web.Api.csproj
│   │   └── Program.cs              # API endpoints
│   │
│   ├── Crypto/                     # Encryption utilities
│   │   ├── Andastra.Web.Crypto.csproj
│   │   ├── WasmEncryption.cs       # AES-GCM implementation
│   │   └── EphemeralKeyGenerator.cs # Key derivation
│   │
│   └── Frontend/                   # Browser client
│       ├── html/
│       │   └── index.html
│       ├── css/
│       │   └── styles.css
│       └── js/
│           ├── crypto.js           # Web Crypto API wrapper
│           ├── filesystem.js       # File System Access API
│           ├── wasm-loader.js      # WASM loading/decryption
│           └── app.js              # Main application logic
│
├── Dockerfile.web                  # Multi-stage Docker build
├── docker-compose.web.yml          # Deployment configuration
└── nginx.conf                      # Reverse proxy config
```

## Building and Deployment

### Prerequisites

- .NET 9.0 SDK
- Docker (for containerized deployment)
- Modern browser with WebAssembly, File System Access API, and Web Crypto API support

### Local Development

1. **Build WASM with AOT:**

```bash
cd src/Andastra/Game.Wasm
dotnet publish -c Release -r browser-wasm /p:RunAOTCompilation=true
```

2. **Build and run API:**

```bash
cd src/Andastra/Web/Api
dotnet run
```

3. **Open browser:**

Navigate to `http://localhost:5000`

### Using Build Script

The automated build script handles the entire pipeline:

```bash
./scripts/build-wasm.sh
```

This script:
1. Builds WASM with AOT compilation
2. (Optional) Runs obfuscation
3. Encrypts the WASM binary
4. Generates encryption keys
5. Outputs build artifacts

### Docker Deployment

#### Simple Deployment

```bash
docker-compose -f docker-compose.web.yml up --build
```

#### Production Deployment with Nginx

```bash
docker-compose -f docker-compose.web.yml --profile with-nginx up --build
```

#### Environment Configuration

Create a `.env` file:

```env
MASTER_SECRET=<base64-encoded-32-byte-secret>
ASPNETCORE_ENVIRONMENT=Production
```

**IMPORTANT**: Store the master secret securely. In production, use:
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault
- Kubernetes Secrets

### Build Configuration

Key WASM project settings in `Andastra.Game.Wasm.csproj`:

```xml
<RuntimeIdentifier>browser-wasm</RuntimeIdentifier>
<RunAOTCompilation>true</RunAOTCompilation>
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>full</TrimMode>
<DebuggerSupport>false</DebuggerSupport>
```

## Browser Client Flow

### 1. Initial Load

```javascript
// Application starts
app.checkBrowserSupport()
app.loadVersionInfo()
```

### 2. Game Folder Selection

```javascript
// User clicks "Select Game Folder"
vfs.selectGameDirectory()
  → Browser shows directory picker
  → User selects KOTOR installation folder
  → Validate chitin.key and .bif files
```

### 3. WASM Loading

```javascript
// Fetch ephemeral key and encrypted WASM in parallel
Promise.all([
  wasmLoader.fetchDecryptionKey(),    // POST /api/runtime/key
  wasmLoader.fetchEncryptedWasm()     // GET /api/runtime/wasm
])
  → Decrypt WASM in memory
  → Wipe encrypted data
  → Instantiate .NET runtime
```

### 4. Game Initialization

```javascript
// Mount virtual filesystem
vfs.mountToWasm('/gamedata')

// Initialize game engine
wasmLoader.initializeGame('/gamedata')
  → Calls C# Program.InitializeGame() via JSExport
  → Game validates file structure
  → Returns success/failure
```

### 5. Game Execution

```javascript
// Start game loop
wasmLoader.startGame()
  → Calls C# Program.StartGame() via JSExport
  → Stride engine begins rendering
  → Game runs entirely client-side
```

## API Endpoints

### POST /api/runtime/key

Issues an ephemeral decryption key.

**Request:**
```http
POST /api/runtime/key HTTP/1.1
Content-Type: application/json
```

**Response:**
```json
{
  "key": "base64-encoded-32-byte-key",
  "validFor": "5 minutes",
  "timestamp": 1702840123
}
```

**Security:**
- Key derived from client IP, User-Agent, and 5-minute time window
- Valid for current and previous time window (clock skew tolerance)
- HMAC-SHA256 for deterministic derivation

### GET /api/runtime/wasm

Serves the encrypted WASM binary.

**Request:**
```http
GET /api/runtime/wasm HTTP/1.1
Accept: application/wasm-encrypted
```

**Response:**
```
Content-Type: application/wasm-encrypted
Cache-Control: public, max-age=3600
[binary data: nonce(12) + tag(16) + ciphertext]
```

### GET /api/version

Returns version and capability information.

**Request:**
```http
GET /api/version HTTP/1.1
```

**Response:**
```json
{
  "api": {
    "version": "1.0.0",
    "build": "1.0.0.0"
  },
  "wasm": {
    "version": "1.0.0",
    "engine": "Stride 4.2",
    "runtime": ".NET 9.0"
  },
  "security": {
    "encryption": "AES-256-GCM",
    "keyDerivation": "HMAC-SHA256",
    "keyValidity": "5 minutes"
  }
}
```

## Virtual Filesystem

The virtual filesystem bridges browser File System Access API to POSIX-like paths for the game engine.

### File Structure Mapping

```
Browser Selection:           WASM Virtual Path:
/Users/player/KOTOR/        /gamedata/
├── chitin.key              ├── chitin.key
└── data/                   └── data/
    ├── file1.bif               ├── file1.bif
    └── file2.bif               └── file2.bif
```

### Implementation Notes

- Uses `FileSystemDirectoryHandle` and `FileSystemFileHandle`
- Read-only access (no writes to user's filesystem)
- Files are read on-demand (not preloaded)
- Supports nested directory structures

## Browser Compatibility

### Required Features

| Feature | Chrome | Edge | Safari | Firefox |
|---------|--------|------|--------|---------|
| WebAssembly | ✅ 57+ | ✅ 16+ | ✅ 11+ | ✅ 52+ |
| File System Access API | ✅ 86+ | ✅ 86+ | ✅ 15.2+ | ❌ (polyfill) |
| Web Crypto API | ✅ 37+ | ✅ 79+ | ✅ 11+ | ✅ 34+ |
| WASM SIMD | ✅ 91+ | ✅ 91+ | ✅ 16.4+ | ✅ 89+ |

### Browser Support Notes

- **Firefox**: File System Access API requires polyfill using `<input type="file" webkitdirectory>`
- **Safari**: Requires iOS 15.2+ / macOS 12.3+
- **Older Browsers**: Not supported due to WebAssembly requirements

## Security Considerations

### Threat Model

**What this protects against:**
- ✅ Source code inspection (AOT-compiled WASM)
- ✅ Casual reverse engineering (encrypted at rest)
- ✅ Direct WASM extraction (requires ephemeral key)
- ✅ Key reuse attacks (time-variant keys)

**What this does NOT protect against:**
- ❌ Determined reverse engineers (WASM can be inspected in memory)
- ❌ Browser debugging tools (DevTools can still observe runtime)
- ❌ Client-side cheating (client controls all game state)

### Best Practices

1. **Store Master Secret Securely**
   - Never commit to source control
   - Use secrets management service
   - Rotate periodically

2. **Use HTTPS in Production**
   - TLS 1.2+ required
   - Valid SSL certificate
   - HSTS headers

3. **Rate Limiting**
   - Limit key requests (10/second per IP)
   - Limit WASM downloads (1/minute per IP)
   - Implement in Nginx or API

4. **Monitoring**
   - Log key requests
   - Alert on suspicious patterns
   - Track WASM download frequency

## Performance Optimization

### WASM Size Reduction

Current optimizations:
- IL trimming: ~30-50% reduction
- AOT compilation: removes JIT overhead
- Brotli compression: ~70% size reduction

### Loading Performance

- Parallel key + WASM fetch: saves ~500ms
- Browser caching: 1-hour cache for WASM
- CDN deployment: reduces latency

### Runtime Performance

- WASM SIMD: enabled for vectorized operations
- No debugging symbols: reduces overhead
- Optimized for speed: `IlcOptimizationPreference=Speed`

## Troubleshooting

### Common Issues

**"WASM file not found"**
- Ensure build completed successfully
- Check wwwroot/wasm/ directory
- Verify file permissions

**"Decryption failed"**
- Key may have expired (5-minute window)
- Check client/server time sync
- Verify master secret is consistent

**"Required game files not found"**
- Ensure chitin.key exists in root
- Verify .bif files in data/ subdirectory
- Check file permissions

**Browser not supported**
- Update to latest browser version
- Check Feature Detection section
- Firefox users may need polyfill

## Future Enhancements

### Planned Features

1. **Pre-AOT Obfuscation**
   - Integration with ConfuserEx or similar
   - Control flow flattening
   - Symbol renaming
   - String encryption

2. **Progressive Loading**
   - Stream WASM in chunks
   - Show loading progress
   - Reduce initial load time

3. **Improved VFS**
   - Emscripten FS integration
   - Better POSIX compatibility
   - Write support (for save games)

4. **Multiplayer Support**
   - WebRTC for peer-to-peer
   - SignalR for server communication
   - Encrypted game state sync

## License and Legal

This implementation follows the Business Source License 1.1 (BSL-1.1). See LICENSE file for details.

**Important**: Production use requires explicit authorization from the licensor.

## Support

For issues and questions:
- GitHub Issues: https://github.com/th3w1zard1/HoloPatcher.NET/issues
- Documentation: This file and inline code comments
- Examples: See `src/Andastra/Web/Frontend/js/` for working implementations

## Acknowledgments

- Built on Stride Game Engine (stride3d.net)
- Uses .NET WebAssembly AOT compilation
- Inspired by modern web game architectures
