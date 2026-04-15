# NuGet Package Distribution

Both `KPatcher.Core` and `KPatcher` are configured to be distributed as NuGet packages, allowing other C# projects to reference them directly instead of using the CLI.

## Building NuGet Packages

### Build Packages Locally

To build NuGet packages for both projects:

```bash
# Build packages in Release configuration
dotnet pack --configuration Release

# Packages will be created in:
# - src/KPatcher.Core/bin/Release/KPatcher.Core.0.1.0.nupkg
# - src/KPatcher.UI/bin/Release/KPatcher.UI.0.1.0.nupkg
```

### Build Individual Packages

```bash
# Build only KPatcher.Core
dotnet pack src/KPatcher.Core/KPatcher.Core.csproj --configuration Release

# Build only KPatcher.UI
dotnet pack src/KPatcher.UI/KPatcher.UI.csproj --configuration Release
```

## Installing Packages

### From Local Package Source

1. Create a local NuGet feed directory:

```bash
mkdir nuget-packages
```

2. Copy the `.nupkg` files to this directory

3. Add the local feed to your project's `nuget.config` or use the `--source` parameter:

```bash
dotnet add package KPatcher.Core --source ./nuget-packages
dotnet add package KPatcher.UI --source ./nuget-packages
```

### From NuGet.org (after publishing)

Once published to NuGet.org, install via:

```bash
dotnet add package KPatcher.Core
dotnet add package KPatcher.UI
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="KPatcher.Core" Version="0.1.0" />
  <PackageReference Include="KPatcher.UI" Version="0.1.0" />
</ItemGroup>
```

## Using the Packages

### Using KPatcher.Core

The core library provides the patching engine:

```csharp
using KPatcher.Core.Patcher;
using KPatcher.Core.Config;
using KPatcher.Core.Logger;

// Create a logger
var logger = new PatchLogger();

// Create installer
var installer = new ModInstaller(
    modPath: @"C:\Mods\MyMod",
    gamePath: @"C:\Games\KOTOR2",
    changesIniPath: @"C:\Mods\MyMod\changes.ini",
    logger: logger
);

// Install the mod
installer.Install();
```

### Using KPatcher.UI

KPatcher.UI can be used as a library for programmatic access:

```csharp
using KPatcher.UI;

// Access the Avalonia UI layer from your application.
```

## Publishing to NuGet.org

1. **Get a NuGet API Key**:
   - Sign in to [nuget.org](https://www.nuget.org)
   - Go to Account Settings -> API Keys
   - Create a new API key

2. **Publish packages**:

```bash
# Publish KPatcher.Core
dotnet nuget push src/KPatcher.Core/bin/Release/KPatcher.Core.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Publish KPatcher.UI
dotnet nuget push src/KPatcher.UI/bin/Release/KPatcher.UI.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

3. **Publish symbol packages** (optional):

```bash
dotnet nuget push src/KPatcher.Core/bin/Release/KPatcher.Core.*.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Package Dependencies

- **KPatcher.Core**: Standalone core patching and format/model library
- **KPatcher.UI**: Depends on KPatcher.Core for the Avalonia UI layer

## Version Management

Update the version in the `.csproj` files:

```xml
<Version>0.1.0</Version>
```

Follow [Semantic Versioning](https://semver.org/):

- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, backward compatible
- **Pre-release**: Use suffixes like `-alpha1`, `-beta1`, `-rc1`

## Notes

- Packages are automatically generated on Release builds when `GeneratePackageOnBuild` is `true`
- Symbol packages (`.snupkg`) are included for debugging support
- The active public package identities are `KPatcher.Core` and `KPatcher.UI`
