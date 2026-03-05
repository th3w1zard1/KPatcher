# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

HoloPatcher.NET is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. It is a 1:1 port of the Python HoloPatcher. See `README.md` for full project details and `docs/` for additional documentation.

### Key commands

| Task | Command |
|---|---|
| Restore | `dotnet restore src/HoloPatcher/HoloPatcher.csproj` |
| Build (Debug) | `dotnet build src/HoloPatcher/HoloPatcher.csproj --configuration Debug --framework net9` |
| Build (Release, with analyzers) | `dotnet build src/CSharpKOTOR/CSharpKOTOR.csproj --configuration Release` |
| Run tests | `dotnet test src/TSLPatcher.Tests/TSLPatcher.Tests.csproj` |
| Run app | `DISPLAY=:1 dotnet run --project src/HoloPatcher/HoloPatcher.csproj --configuration Debug --framework net9` |

### Gotchas

- **Do NOT build the .sln file directly.** The solution (`HoloPatcher.sln`) references many projects that don't exist on disk (NCSDecomp, HolocronToolset.NET, OdysseyRuntime sub-projects). Build individual `.csproj` files instead.
- **`--framework net9` is required** when running the HoloPatcher executable via `dotnet run` because the project multi-targets in Release mode and `dotnet run` cannot pick a default.
- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Pre-existing test failures**: ~893 of 976 tests fail with "Tests marked with Timeout are only supported for async tests" due to a version mismatch between xunit 2.9.3 and xunit.runner.visualstudio 3.1.5. This is a known pre-existing issue, not caused by environment setup. The 83 passing tests cover the core functionality.
- **NCS Roundtrip tests** require `nwnnsscomp.exe` (Windows-only external compiler) and will always fail on Linux.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.
