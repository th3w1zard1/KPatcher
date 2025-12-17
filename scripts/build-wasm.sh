#!/bin/bash
# Build script for WASM with AOT compilation and encryption

set -e

echo "============================================"
echo "Andastra WASM Build Pipeline"
echo "============================================"

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WASM_PROJECT="$PROJECT_ROOT/src/Andastra/Game.Wasm/Andastra.Game.Wasm.csproj"
CRYPTO_PROJECT="$PROJECT_ROOT/src/Andastra/Web/Crypto/Andastra.Web.Crypto.csproj"
OUTPUT_DIR="$PROJECT_ROOT/build/wasm"
ENCRYPTED_DIR="$PROJECT_ROOT/build/encrypted"

# Clean previous builds
echo "[1/5] Cleaning previous builds..."
rm -rf "$OUTPUT_DIR"
rm -rf "$ENCRYPTED_DIR"
mkdir -p "$OUTPUT_DIR"
mkdir -p "$ENCRYPTED_DIR"

# Build WASM with AOT
echo "[2/5] Building WASM with AOT compilation..."
echo "This may take several minutes..."

dotnet publish "$WASM_PROJECT" \
    -c Release \
    -o "$OUTPUT_DIR" \
    -r browser-wasm \
    /p:RunAOTCompilation=true \
    /p:PublishTrimmed=true \
    /p:TrimMode=full \
    /p:BlazorEnableCompression=true

if [ $? -ne 0 ]; then
    echo "ERROR: WASM build failed"
    exit 1
fi

echo "✓ WASM build completed"

# Check if WASM file exists
WASM_FILE="$OUTPUT_DIR/Andastra.Game.Wasm.wasm"
if [ ! -f "$WASM_FILE" ]; then
    echo "ERROR: WASM file not found at $WASM_FILE"
    exit 1
fi

WASM_SIZE=$(du -h "$WASM_FILE" | cut -f1)
echo "✓ WASM size: $WASM_SIZE"

# Optional: Run obfuscation (if tool is installed)
echo "[3/5] Checking for obfuscation tools..."
if command -v ConfuserEx &> /dev/null; then
    echo "Running obfuscation..."
    # Add obfuscation command here
    echo "⚠ Obfuscation skipped (configure ConfuserEx or similar tool)"
else
    echo "⚠ No obfuscation tool found, skipping"
fi

# Build encryption tool
echo "[4/5] Building encryption tool..."
dotnet build "$CRYPTO_PROJECT" -o "$OUTPUT_DIR/crypto"

# Encrypt WASM
echo "[5/5] Encrypting WASM binary..."

cat > "$OUTPUT_DIR/encrypt.csx" << 'EOF'
#r "crypto/Andastra.Web.Crypto.dll"

using System;
using System.IO;
using Andastra.Web.Crypto;

var key = WasmEncryption.GenerateKey();
var wasmPath = Args[0];
var outputPath = Args[1];
var keyPath = Args[2];

Console.WriteLine($"Encrypting: {wasmPath}");
WasmEncryption.EncryptWasmFile(wasmPath, outputPath, key);

File.WriteAllBytes(keyPath, key);
Console.WriteLine($"Encrypted WASM saved to: {outputPath}");
Console.WriteLine($"Master key saved to: {keyPath}");
Console.WriteLine($"Key (Base64): {Convert.ToBase64String(key)}");
EOF

dotnet script "$OUTPUT_DIR/encrypt.csx" \
    "$WASM_FILE" \
    "$ENCRYPTED_DIR/Andastra.Game.Wasm.wasm.encrypted" \
    "$ENCRYPTED_DIR/master.key"

if [ $? -ne 0 ]; then
    echo "ERROR: Encryption failed"
    exit 1
fi

echo "✓ WASM encrypted successfully"

# Display results
echo ""
echo "============================================"
echo "Build Summary"
echo "============================================"
echo "WASM Output: $OUTPUT_DIR"
echo "Encrypted Output: $ENCRYPTED_DIR"
echo ""
echo "Next steps:"
echo "1. Copy encrypted WASM to API wwwroot/wasm/"
echo "2. Store master key securely (DO NOT commit to git)"
echo "3. Build and deploy API"
echo ""
echo "To deploy with Docker:"
echo "  docker-compose -f docker-compose.web.yml up --build"
echo "============================================"
