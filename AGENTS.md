# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

KPatcher.NET is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. It is a 1:1 port of the Python KPatcher. See `README.md` for full project details and `docs/` for additional documentation.

### Key commands

| Task | Command |
|---|---|
| Restore | `dotnet restore src/KPatcher/KPatcher.csproj` |
| Build (Debug) | `dotnet build src/KPatcher/KPatcher.csproj --configuration Debug --framework net9` |
| Build (Release, with analyzers) | `dotnet build src/KPatcher.Core/KPatcher.Core.csproj --configuration Release` |
| Run tests | `dotnet test src/KPatcher.Tests/KPatcher.Tests.csproj` |
| Run app | `DISPLAY=:1 dotnet run --project src/KPatcher/KPatcher.csproj --configuration Debug --framework net9` |

### Gotchas

- **Do NOT build the .sln file directly.** The solution (`KPatcher.sln`) references many projects that don't exist on disk (NCSDecomp, HolocronToolset.NET, OdysseyRuntime sub-projects). Build individual `.csproj` files instead.
- **`--framework net9` is required** when running the KPatcher executable via `dotnet run` because the project multi-targets in Release mode and `dotnet run` cannot pick a default.
- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Test results on Linux**: 936/976 pass. 40 failures are platform-specific (Windows path tests, Linux exception-type differences, NCS external compiler requirement). These pass on Windows except NCS roundtrip tests.
- **NCS Roundtrip tests** require `nwnnsscomp.exe` (Windows-only external compiler) and will always fail on Linux.
- **CI workflows** target specific `.csproj` files, NOT the `.sln`. The solution references 11 missing projects and cannot be used with `dotnet restore/build/test`.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.
