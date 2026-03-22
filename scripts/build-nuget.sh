#!/bin/bash
# Build NuGet package for KPatcher.Core (app host is not packaged)
# Usage: ./build-nuget.sh [--publish] [--source <feed-url>] [--api-key <key>]

set -e

PUBLISH=false
SOURCE="https://api.nuget.org/v3/index.json"
API_KEY=""
CONFIGURATION="Release"

while [[ $# -gt 0 ]]; do
    case $1 in
        --publish)
            PUBLISH=true
            shift
            ;;
        --source)
            SOURCE="$2"
            shift 2
            ;;
        --api-key)
            API_KEY="$2"
            shift 2
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo "Building NuGet packages..."

echo ""
echo "Building KPatcher.Core..."
dotnet pack src/KPatcher.Core/KPatcher.Core.csproj --configuration "$CONFIGURATION" --no-build

TSL_CORE_PACKAGE=$(find "src/KPatcher.Core/bin/$CONFIGURATION" -name "*.nupkg" | head -n 1)

if [ -z "$TSL_CORE_PACKAGE" ]; then
    echo "KPatcher.Core package not found!"
    exit 1
fi

echo ""
echo "KPatcher.Core package created: $TSL_CORE_PACKAGE"

if [ "$PUBLISH" = true ]; then
    if [ -z "$API_KEY" ]; then
        echo "Error: --api-key is required when using --publish"
        exit 1
    fi

    echo ""
    echo "Publishing packages to $SOURCE..."

    echo "Publishing KPatcher.Core..."
    dotnet nuget push "$TSL_CORE_PACKAGE" --api-key "$API_KEY" --source "$SOURCE" --skip-duplicate

    TSL_CORE_SYMBOLS=$(find "src/KPatcher.Core/bin/$CONFIGURATION" -name "*.snupkg" | head -n 1)

    if [ -n "$TSL_CORE_SYMBOLS" ]; then
        echo "Publishing KPatcher.Core symbols..."
        dotnet nuget push "$TSL_CORE_SYMBOLS" --api-key "$API_KEY" --source "$SOURCE" --skip-duplicate
    fi

    echo ""
    echo "Packages published successfully!"
else
    echo ""
    echo "Packages built successfully!"
    echo "To publish, run: ./build-nuget.sh --publish --api-key YOUR_API_KEY"
fi
