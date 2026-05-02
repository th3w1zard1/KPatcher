# KPatcher

A direct 1:1 port of KPatcher from Python to C#/.NET with Avalonia UI framework.

## Port Philosophy

This project is a **faithful, line-by-line translation** of the original Python implementation. The codebase maintains strict functional equivalence with the source material, with **no creative changes, architectural modifications, or feature additions** beyond what is necessary for language and framework translation.

### Porting Standards

- **1:1 Functional Equivalence**: Every function, class, and method maintains identical behavior to the Python source
- **Preserved Logic**: All business logic, algorithms, and data structures are translated directly without modification
- **Comment Preservation**: Original Python comments and documentation are preserved where applicable
- **No Feature Creep**: No additional features or "improvements" beyond the original specification
- **Framework Translation Only**: Changes are limited to:
  - Python -> C# syntax translation
  - Tkinter -> Avalonia UI framework adaptation
  - Python standard library -> .NET equivalent APIs
  - Threading model adaptation (Python threading -> C# async/await)

The only exception to strict 1:1 parity is RTF rendering: the C# version attempts to render RTF content natively using Avalonia's RichTextBox control before falling back to stripped plain text, whereas the Python version strips RTF immediately due to Tkinter limitations.

## Project Structure

- **KPatcher** - Main Avalonia desktop application (also headless CLI)
- **KPatcher.UI** - Packable Avalonia UI library
- **KPatcher.Core** - Packable core patching engine and data/model library
- **KCompiler.Core** / **KCompiler.NET** (`kcompiler`) - Managed NSS->NCS compiler CLI
- **NCSDecomp.Core** / **NCSDecomp.NET** (NCSDecompCLI) / **NCSDecomp.UI** - Managed NCS->NSS decompiler (DeNCS port)
- **KEditChanges** / **KEditChanges.NET** (`keditchanges-cli`) - Umbrella CLI (compile + decomp + placeholder info)

Reverse-engineering references:

- [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md)
- [docs/NWNNSSCOMP_RE.md](docs/NWNNSSCOMP_RE.md)

See [AGENTS.md](AGENTS.md) for build/publish commands and **“Which binary do I run?”**

## Features

### Implemented

- ✅ Basic UI structure with Avalonia
- ✅ Logger system (PatchLogger)
- ✅ Configuration models (PatcherConfig, LogLevel)
- ✅ Memory system (PatcherMemory)
- ✅ Token system (TokenUsage, NoTokenUsage, TokenUsage2DA, TokenUsageTLK)
- ✅ Namespace support (PatcherNamespace)
- ✅ Menu system (Tools, Help)
- ✅ Progress tracking
- ✅ Modification infrastructure (PatcherModification base class)
- ✅ 2DA modification system:
  - RowValue classes (Constant, 2DAMemory, TLKMemory, High, RowIndex, RowLabel, RowCell)
  - Target resolution (RowIndex, RowLabel, LabelColumn)
  - ChangeRow2DA, AddRow2DA, CopyRow2DA, AddColumn2DA
  - Modifications2DA container
- ✅ Comprehensive unit tests (xUnit + Fluent Assertions 7.x, Apache 2.0)
- ✅ **Localized config files**: optional `changes.<lang>.ini` / `changes.<lang>.yaml` and `namespaces.<lang>.ini` (e.g. `changes.de.ini` for German UI); see [docs/LOCALIZED_CONFIG_FILES.md](docs/LOCALIZED_CONFIG_FILES.md).

### TODO

- ✅ Permission fixing tools (Windows ReadOnly + Unix chmod)
- ✅ iOS case sensitivity fixing (Tools menu uses full recursive rename)
- ✅ Auto-update system (Help -> Check for updates; NetSparkle disabled on .NET 5+)
- ✅ RTF file handling (single RtfStripper in Core; InstallLogWriter tests)
- ✅ Test coverage (SystemHelpers, RtfStripper, InstallLogWriter)

## Requirements

- .NET 9 SDK (primary TFM; some projects multi-target older frameworks)
- Avalonia 11.x (see project `PackageReference` versions)

## Vendor submodules

This repository uses git submodules for reference and test assets:

| Path | Purpose |
|------|---------|
| `vendor/PyKotor` | Parity reference (HoloPatcher, PyKotor library). |
| `vendor/Vanilla_KOTOR_Script_Source` | Decompiled vanilla NSS (K1/TSL) for compile/roundtrip tests. |
| `vendor/DeNCS` | NCS decompiler reference (Java); C# decoder port lives under `src/KPatcher.Core/Formats/NCS/Decompiler/`. |

Additional checked-in vendor reference:

| Path | Purpose |
|------|---------|
| `vendor/TSLPatcher` | Reverse-engineered Delphi reference source for `TSLPatcher.exe` and `ChangeEdit.exe`; verification workflow is documented in [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md). |

Clone with submodules to run vanilla NSS compile tests and to have full parity references:

```bash
git clone --recurse-submodules <repo-url>
# or, for an existing clone:
git submodule update --init --recursive
```

Note: `vendor/PyKotor` has nested submodules; if `submodule update --recursive` fails on a missing URL (e.g. Kotor-3D-Model-Converter), init only the top-level submodules.

## Testing

The project includes comprehensive unit tests covering all core functionality. See [docs/TESTING.md](docs/TESTING.md) for runsettings, tiers, and harness commands.

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "FullyQualifiedName~PatcherMemoryTests"
```

### Test Coverage

Current test coverage includes:

- ✅ PatcherMemory (token storage and retrieval)
- ✅ PatchLogger (logging with different levels)
- ✅ PatcherConfig (configuration management)
- ✅ LogLevel (enum behavior)
- ✅ PatcherNamespace (namespace handling)
- ✅ 2DA modifications
- ✅ GFF modifications
- ✅ TLK modifications
- ✅ SSF modifications
- 🚧 NSS/NCS modifications (roundtrip tests may require `nwnnsscomp.exe` on Windows)
- ✅ Managed round-trip compare (`RoundTripUtil.CompareManagedRecompileToOriginalDecoderText`) — xUnit tests in `tests/KPatcher.Tests`
- ✅ Config reader/INI parsing
- ✅ SystemHelpers (permission fixing, case sensitivity)
- ✅ RtfStripper (RTF-to-plain-text)
- ✅ InstallLogWriter (Error/Warning prefixes, log file creation)

**Platform-specific notes:** Some tests are Windows-only (e.g. permission ReadOnly clearing). NCS roundtrip and external compiler tests may be skipped or fail when `nwnnsscomp.exe` is not available. See [docs/TESTING.md](docs/TESTING.md) for details.

## Building

From the repository root:

```bash
dotnet restore src/KPatcher.UI/KPatcher.UI.csproj
dotnet build src/KPatcher.UI/KPatcher.UI.csproj --configuration Debug --framework net9.0
```

Build **all** main apps and tool CLIs in one step:

```bash
dotnet build KPatcher.sln --configuration Debug
```

## Publishing (KPatcher + sidecar tools)

A **Release** publish of KPatcher runs **`PublishBundledCliTools`**, which also publishes **kcompiler**, **NCSDecompCLI**, and **keditchanges-cli** into the same output folder (same runtime identifier and self-contained settings as KPatcher). Example:

```bash
dotnet publish src/KPatcher.UI/KPatcher.UI.csproj -c Release -f net9.0
```

## Running

```bash
dotnet run --project src/KPatcher.UI/KPatcher.UI.csproj --configuration Debug --framework net9.0
```

## Architecture

### KPatcher.Core

The core library contains all the patching logic independent of UI:

- **Config/** - Configuration models and parsing
- **Logger/** - Logging system
- **Memory/** - Token memory for patches
- **Namespaces/** - Namespace management
- **Mods/** - Modification operations (GFF, 2DA, TLK, etc.)
- **Patcher/** - Main installation engine

### KPatcher

The Avalonia UI application follows MVVM pattern:

- **Views/** - XAML views
- **ViewModels/** - View models with business logic
- **Services/** - Application services

## NuGet Packages

Release builds generate the public library packages consumed by other .NET projects:

- **KPatcher.Core** - Core patching engine and format/model library
- **KPatcher.UI** - Avalonia UI layer built on top of `KPatcher.Core`

Example consumption:

```xml
<ItemGroup>
  <PackageReference Include="KPatcher.Core" Version="0.1.0" />
  <PackageReference Include="KPatcher" Version="0.1.0" />
  <PackageReference Include="KPatcher.UI" Version="0.1.0" />
</ItemGroup>
```

## Port Implementation Details

This codebase is a direct translation from the original Python/Tkinter implementation located in `vendor/PyKotor/Tools/KPatcher/src/kpatcher/`. The following technical adaptations were made solely for language and framework compatibility:

1. **UI Framework**: Tkinter -> Avalonia (required for cross-platform .NET UI)
2. **Language**: Python -> C# (.NET) (syntax translation only, no logic changes)
3. **Architecture**: Maintains original structure; MVVM pattern used only where Avalonia requires it
4. **Threading**: Python threading -> C# Tasks/async-await (equivalent functionality)
5. **Logging**: Original observable pattern preserved in C# implementation

**Important**: All functional behavior, error handling, edge cases, and business logic remain identical to the Python source. This is not a rewrite or reimplementation—it is a faithful translation.

## Contributing

When contributing to this port:

1. **Reference the original Python code** in `vendor/PyKotor/Tools/KPatcher/src/kpatcher/`
2. **Maintain strict 1:1 functional equivalence**—no creative changes or feature additions
3. **Preserve original logic**—translate, don't reimplement
4. **Follow C# coding conventions** for syntax and style
5. **Use async/await** for I/O operations (equivalent to Python's async patterns)
6. **Add XML documentation comments** that reference the original Python implementation
7. **Document any deviations** from the source material (should be minimal to none)

### Porting Guidelines

- If the Python code has a bug, port the bug (then file a separate issue to fix it in both versions)
- If the Python code uses an inefficient algorithm, port the inefficient algorithm
- If the Python code has a TODO comment, preserve the TODO in the C# version
- The goal is **functional parity**, not improvement

This ensures that bug fixes, feature requests, and behavioral changes can be synchronized between the Python and C# implementations.

## CI/CD

This project uses GitHub Actions for continuous integration and automated releases. See [GITHUB_ACTIONS_SETUP.md](GITHUB_ACTIONS_SETUP.md) for details.

## License

This project is licensed under the GNU Lesser General Public License, version 3 or any later version (LGPL-3.0-or-later). See the [LICENSE](LICENSE) file for details.
