# NuGet Package Distribution

Both `TSLPatcher.Core` and `HoloPatcher` are configured to be distributed as NuGet packages, allowing other C# projects to reference them directly instead of using the CLI.

## Building NuGet Packages

### Build Packages Locally

To build NuGet packages for both projects:

```bash
# Build packages in Release configuration
dotnet pack --configuration Release

# Packages will be created in:
# - src/TSLPatcher.Core/bin/Release/TSLPatcher.Core.2.0.0-alpha3.nupkg
# - src/HoloPatcher.UI/bin/Release/HoloPatcher.UI.2.0.0-alpha3.nupkg
```

### Build Individual Packages

```bash
# Build only TSLPatcher.Core
dotnet pack src/TSLPatcher.Core/TSLPatcher.Core.csproj --configuration Release

# Build only HoloPatcher.UI
dotnet pack src/HoloPatcher.UI/HoloPatcher.UI.csproj --configuration Release
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
dotnet add package TSLPatcher.Core --source ./nuget-packages
dotnet add package HoloPatcher.UI --source ./nuget-packages
```

### From NuGet.org (after publishing)

Once published to NuGet.org, install via:

```bash
dotnet add package TSLPatcher.Core
dotnet add package HoloPatcher.UI
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="TSLPatcher.Core" Version="2.0.0-alpha3" />
  <PackageReference Include="HoloPatcher.UI" Version="2.0.0-alpha3" />
</ItemGroup>
```

## Using the Packages

### Using TSLPatcher.Core

The core library provides the patching engine:

```csharp
using TSLPatcher.Core.Patcher;
using TSLPatcher.Core.Config;
using TSLPatcher.Core.Logger;

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

### Using HoloPatcher.UI

HoloPatcher.UI can be used as a library for programmatic access:

```csharp
using HoloPatcher.UI;

// Access the Avalonia UI layer from your application.
```

## Publishing to NuGet.org

1. **Get a NuGet API Key**:
   - Sign in to [nuget.org](https://www.nuget.org)
   - Go to Account Settings → API Keys
   - Create a new API key

2. **Publish packages**:

```bash
# Publish TSLPatcher.Core
dotnet nuget push src/TSLPatcher.Core/bin/Release/TSLPatcher.Core.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Publish HoloPatcher.UI
dotnet nuget push src/HoloPatcher.UI/bin/Release/HoloPatcher.UI.*.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

3. **Publish symbol packages** (optional):

```bash
dotnet nuget push src/TSLPatcher.Core/bin/Release/TSLPatcher.Core.*.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Package Dependencies

- **TSLPatcher.Core**: Standalone core patching and format/model library
- **HoloPatcher.UI**: Depends on TSLPatcher.Core for the Avalonia UI layer

## Version Management

Update the version in the `.csproj` files:

```xml
<Version>2.0.0-alpha3</Version>
```

Follow [Semantic Versioning](https://semver.org/):

- **Major**: Breaking changes
- **Minor**: New features, backward compatible
- **Patch**: Bug fixes, backward compatible
- **Pre-release**: Use suffixes like `-alpha1`, `-beta1`, `-rc1`

## Notes

- Packages are automatically generated on Release builds when `GeneratePackageOnBuild` is `true`
- Symbol packages (`.snupkg`) are included for debugging support
- The active public package identities are `TSLPatcher.Core` and `HoloPatcher.UI`
