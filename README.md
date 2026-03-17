# KPatcher.NET

A direct 1:1 port of KPatcher from Python to C#/.NET with Avalonia UI framework.

## Port Philosophy

This project is a **faithful, line-by-line translation** of the original Python implementation. The codebase maintains strict functional equivalence with the source material, with **no creative changes, architectural modifications, or feature additions** beyond what is necessary for language and framework translation.

### Porting Standards

- **1:1 Functional Equivalence**: Every function, class, and method maintains identical behavior to the Python source
- **Preserved Logic**: All business logic, algorithms, and data structures are translated directly without modification
- **Comment Preservation**: Original Python comments and documentation are preserved where applicable
- **No Feature Creep**: No additional features or "improvements" beyond the original specification
- **Framework Translation Only**: Changes are limited to:
  - Python → C# syntax translation
  - Tkinter → Avalonia UI framework adaptation
  - Python standard library → .NET equivalent APIs
  - Threading model adaptation (Python threading → C# async/await)

The only exception to strict 1:1 parity is RTF rendering: the C# version attempts to render RTF content natively using Avalonia's RichTextBox control before falling back to stripped plain text (matching Python's behavior), whereas the Python version strips RTF immediately due to Tkinter limitations.

## Project Structure

- **KPatcher** - Main Avalonia desktop application
- **KPatcher.UI** - Packable Avalonia UI library
- **KPatcher.Core** - Packable core patching engine and data/model library

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
- ✅ Comprehensive unit tests (xUnit + FluentAssertions)

### TODO

- ⏳ Permission fixing tools
- ⏳ iOS case sensitivity fixing
- ⏳ Auto-update system
- ⏳ RTF file handling
- ⏳ Complete test coverage

## Requirements

- .NET 8.0 SDK
- Avalonia 11.0.10

## Testing

The project includes comprehensive unit tests covering all core functionality. See [TESTING.md](TESTING.md) for detailed information.

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
- 🚧 NSS/NCS modifications
- ✅ Config reader/INI parsing

## Building

```bash
cd Tools/KPatcher.NET
dotnet restore
dotnet build
```

## Running

```bash
dotnet run --project src/KPatcher/KPatcher.csproj
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

1. **UI Framework**: Tkinter → Avalonia (required for cross-platform .NET UI)
2. **Language**: Python → C# (.NET) (syntax translation only, no logic changes)
3. **Architecture**: Maintains original structure; MVVM pattern used only where Avalonia requires it
4. **Threading**: Python threading → C# Tasks/async-await (equivalent functionality)
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
