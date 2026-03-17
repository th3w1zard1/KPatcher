#!/bin/bash
# Build script for KPatcher.NET

echo "Building KPatcher.NET..."

# Restore dependencies
dotnet restore KPatcher.sln

# Build the solution
dotnet build KPatcher.sln --configuration Release

# Run tests
echo "Running tests..."
dotnet test KPatcher.sln --configuration Release --no-build

echo "Build complete!"

