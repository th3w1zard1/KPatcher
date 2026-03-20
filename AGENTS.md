# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

KPatcher.NET is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. It is a 1:1 port of the Python KPatcher. See `README.md` for full project details and `docs/` for additional documentation.

### Key commands

| Task | Command |
|---|---|
| Restore | `dotnet restore src/KPatcher/KPatcher.csproj` |
| Build (Debug) | `dotnet build src/KPatcher/KPatcher.csproj --configuration Debug --framework net9.0` |
| Build (Release, with analyzers) | `dotnet build src/KPatcher.Core/KPatcher.Core.csproj --configuration Release` |
| Build KCompiler.Core | `dotnet build src/KCompiler.Core/KCompiler.Core.csproj --configuration Debug --framework net9.0` |
| Run KCompiler CLI | `dotnet run --project src/KCompiler.NET/KCompiler.NET.csproj --configuration Debug -- -c script.nss -o out.ncs -g 1` (note `--` before app args) |
| Pack KCompiler.Core (NuGet) | `dotnet build src/KCompiler.Core/KCompiler.Core.csproj --configuration Release` (packs via `GeneratePackageOnBuild`) or `dotnet pack ... --configuration Release` after a Release build |
| Run tests | `dotnet test src/KPatcher.Tests/KPatcher.Tests.csproj` |
| Run app | `DISPLAY=:1 dotnet run --project src/KPatcher/KPatcher.csproj --configuration Debug --framework net9.0` |

### Gotchas

- **Do NOT build the .sln file directly.** The solution (`KPatcher.sln`) references many projects that don't exist on disk (NCSDecomp, HolocronToolset.NET, OdysseyRuntime sub-projects). Build individual `.csproj` files instead.
- **`--framework net9.0` is required** when running the KPatcher executable via `dotnet run` because the project multi-targets in Release mode and `dotnet run` cannot pick a default.
- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Test results on Linux**: 936/976 pass. 40 failures are platform-specific (Windows path tests, Linux exception-type differences, NCS external compiler requirement). These pass on Windows except NCS roundtrip tests.
- **NSS→NCS** is implemented in **`KCompiler.Core`** (managed compiler, cross-platform). `KPatcher.Core` references that package; the patcher prefers managed compilation first and may fall back to `nwnnsscomp.exe` on Windows only if managed compile fails.
- **NCS Roundtrip tests** may still involve external tooling where tests explicitly shell out to `nwnnsscomp.exe` on Windows.
- **CI workflows** target specific `.csproj` files, NOT the `.sln`. The solution references 11 missing projects and cannot be used with `dotnet restore/build/test`.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.

## Learned User Preferences

- When changing how mods are applied (2DA, TLK, GFF, install paths, or related patcher flows), treat mismatches with HoloPatcher or PyKotor as potential KPatcher bugs until parity with the vendored reference code is checked.
- For UI features described as matching HoloPatcher (for example install auto-detection), follow the same registry and default-path discovery approach as vendored HoloPatcher unless there is a deliberate, documented reason to differ.
- Keep the main log or status area readable at a comfortable font size, with per-line color highlighting by log level (error/warning/note) and support for normal text selection, copy, and select-all like a typical desktop text surface.

## Learned Workspace Facts

- Parity reference sources live under `vendor/PyKotor/Tools/HoloPatcher/src/holopatcher` and `vendor/PyKotor/Libraries/PyKotor/src/pykotor/tslpatcher/`.
- For TSLPatcher reverse-engineering and parity, use `docs/TSLPATCHER_RE.md` and vendor references; the goal is for `src` to match TSLPatcher.exe behavior.
- When using AgentDecompile MCP with binaries in this workspace: use the **open** tool with the program path (e.g. `/TSLPatcher.exe`) to open an already-imported binary, not open-project.
- nwnnsscomp.exe maps to KCompiler (KCompiler.Core); ChangeEdit.exe (from ChangeEdit.exe.gzf) maps to KEditChanges.
- Keep all TSLPatcher reverse-engineering findings in a single doc (`docs/TSLPATCHER_RE.md`); do not create separate RE progress or deliverable markdown files.

### Localization

- **Supported UI languages**: English (default), Spanish, German, French, Russian, Polish. Language is chosen at startup from `CultureInfo.CurrentUICulture`; if the OS language is not in this set, the app falls back to English.
- **Resource files**: Core patcher/parity strings live in **`KPatcher.Core/Resources/PatcherResources.resx`** (and `PatcherResources.{es,de,fr,ru,pl}.resx`). UI strings live in **`KPatcher.UI/Resources/UIResources.resx`** (and `UIResources.{es,de,fr,ru,pl}.resx`). Only the default `.resx` has an associated Designer; satellite files are embedded resources.
- **TSLPatcher parity**: User-facing patcher messages that must match TSLPatcher.exe are in `PatcherResources` with English as the default; `TSLPatcherMessages` is a thin facade over `PatcherResources`. Do not change the English default text for parity-critical keys.
- **Install log**: The prefixes `"Error: "` and `"Warning: "` in `InstallLogWriter` must remain literal for KOTORModSync; only the message content is localized via `PatcherResources` at call sites.
- **Adding a language**: Add `PatcherResources.xx.resx` and `UIResources.xx.resx` with the same keys and translated values; add the two-letter code to the supported list in `App.axaml.cs` (`OnFrameworkInitializationCompleted`).
- **Localized config files**: changes.ini and namespaces.ini support language-specific variants (<basename>.<two-letter-lang>.ini or .yaml); see `docs/LOCALIZED_CONFIG_FILES.md`.

### Notes

- **KOTORModSync compatibility:** The literal prefixes `"Error: "` and `"Warning: "` in `InstallLogWriter` must remain unchanged. Only the message content should be localized.