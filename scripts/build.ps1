# Build script for KPatcher.NET (PowerShell)

Write-Host "Building KPatcher.NET..." -ForegroundColor Green

# Restore dependencies
dotnet restore KPatcher.sln

# Build the solution
dotnet build KPatcher.sln --configuration Release

# Run tests
Write-Host "Running tests..." -ForegroundColor Cyan
dotnet test KPatcher.sln --configuration Release --no-build

Write-Host "Build complete!" -ForegroundColor Green

