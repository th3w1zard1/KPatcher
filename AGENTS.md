# AGENTS.md

## Cursor Cloud specific instructions

### Project overview

KPatcher is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. It is a 1:1 port of the Python HoloPatcher tool. See `README.md` for full project details and `docs/` for additional documentation.

### Key commands

| Task | Command |
|---|---|
| Restore | `dotnet restore src/KPatcher.UI/KPatcher.UI.csproj` |
| Build solution (all main projects) | `dotnet build KPatcher.sln --configuration Debug` — includes **KPatcher** (executable project `KPatcher.UI`), **kcompiler** (`KCompiler.NET`), **NCSDecomp** (Core / CLI / UI), **keditchanges-cli** (`KEditChanges.NET`), **KEditChanges** lib |
| Build (Debug) | `dotnet build src/KPatcher.UI/KPatcher.UI.csproj --configuration Debug --framework net9.0` |
| Build (Release, with analyzers) | `dotnet build src/KPatcher.Core/KPatcher.Core.csproj --configuration Release` |
| Build KCompiler.Core | `dotnet build src/KCompiler.Core/KCompiler.Core.csproj --configuration Debug --framework net9.0` |
| Run KCompiler CLI | `dotnet run --project src/KCompiler.NET/KCompiler.NET.csproj --configuration Debug -- -c script.nss -o out.ncs -g 1` (note `--` before app args) |
| Pack KCompiler.Core (NuGet) | `dotnet build src/KCompiler.Core/KCompiler.Core.csproj --configuration Release` (packs via `GeneratePackageOnBuild`) or `dotnet pack ... --configuration Release` after a Release build |
| Run tests | **Always** use the timeout wrapper (kills `dotnet` when time expires): `.\scripts\DotnetTest.ps1 KPatcher.sln -c Debug` (Windows PowerShell; pass all `dotnet test` args after optional `-TimeoutSeconds N`) or `./scripts/dotnet-test.sh KPatcher.sln -c Debug` (Unix). Single project: `.\scripts\DotnetTest.ps1 tests\KPatcher.Tests\KPatcher.Tests.csproj`. Override seconds with `-TimeoutSeconds` / env `DOTNET_TEST_TIMEOUT_SECONDS`; default **7200**. Exit **124** = timeout. Do **not** run bare `dotnet test` from agents/automation. |
| Run app | `DISPLAY=:1 dotnet run --project src/KPatcher.UI/KPatcher.UI.csproj --configuration Debug --framework net9.0` |
| Publish KPatcher + sidecar CLIs | `dotnet publish src/KPatcher.UI/KPatcher.UI.csproj -c Release -f net9.0` — **`PublishBundledCliTools`** (after `Publish`) merges **kcompiler** and **NCSDecompCLI** into the same **`PublishDir`** via staging under `obj/.../sidecar_*` (same RID/self-contained as KPatcher; **`net9.0` only**). Publish **keditchanges-cli** separately from `KEditChanges.NET`. |
| Publish KPatcher (**match CI release layout**) | `dotnet publish src/KPatcher.UI/KPatcher.UI.csproj -c Release -f net9.0 -r <rid> --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=true -o dist/build/net9.0/<rid>/` — same MSBuild properties as `.github/workflows/build-all-platforms.yml` / `test-builds.yml` (substitute `<rid>`, e.g. `win-x64`). **GitHub release zips:** `KPatcher-<version>-<platform>-<arch>.zip` (not raw RID in the filename). |
| Bundled apphosts (with KPatcher **net9** publish) | **Windows:** `KPatcher.exe`, `kcompiler.exe`, `NCSDecompCLI.exe` in **`PublishDir`**. **Linux / macOS:** extensionless `KPatcher`, `kcompiler`, `NCSDecompCLI`. **`net48`** KPatcher publish does **not** run `PublishBundledCliTools` — no bundled sidecars. |
| CI verify (workflows) | **`test-builds`** / **`build-all-platforms`** assert those apphosts exist (non-empty) and run **`--help`** with exit code **0** after publish (`chmod +x` on Unix before invoking). |

### Which binary do I run?

| Need | Executable / entry |
|------|----------------------|
| Install mods (GUI or HoloPatcher-style CLI) | **KPatcher** (`src/KPatcher.UI`, assembly/output name **KPatcher**) — `--help` for flags |
| NSS → NCS (managed, nwnnsscomp-style args) | **kcompiler** (`KCompiler.NET`) or `keditchanges-cli compile …` |
| NCS → NSS (managed DeNCS port) | **NCSDecompCLI** (`NCSDecomp.NET`) or `keditchanges-cli ncsdecomp …` |
| One binary with subcommands (compile / decomp / info) | **keditchanges-cli** (`KEditChanges.NET`) |

### Gotchas

- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Test results on Linux**: 936/976 pass. 40 failures are platform-specific (Windows path tests, Linux exception-type differences, NCS external compiler requirement). These pass on Windows except NCS roundtrip tests.
- **NSS→NCS** is implemented in **`KCompiler.Core`** (managed compiler, cross-platform). `KPatcher.Core` references that package; primary flows use managed compilation only (see `.cursor/commands/lfg.md` policy: no registry spoofing, no `nwnnsscomp.exe` as a default dependency).
- **NCS→NSS (managed DeNCS port):** `NCSDecomp.Core` + **`KPatcher.Core.Formats.NCS.Decompiler.NCSManagedDecompiler`** (full pipeline). Decoder-only token string: **`NCSDecompiler.Decompile`** in `KCompiler.Core`. `NCSDecomp.Core` references `KCompiler.Core` only; `KPatcher.Core` references `NCSDecomp.Core` so there is no project cycle.
- **`NCSDecompCliRoundTripTest`** (traits `ExternalCompiler` / `Vendor` / `DeNCSRoundTrip`) **always runs** with `dotnet test` on `KPatcher.Tests`: exhaustive vanilla NSS↔NCS using **`nwnnsscomp.exe`**, managed decompile, KCompiler bytecode parity, and strict text/bytecode checks. It is **long-running** (often **90+ minutes** for the full suite) and needs **`nwnnsscomp`**, **`include/*.nss`**, and **`Vanilla_KOTOR_Script_Source`** (under `test-work/` or clone). Use **`DotnetTest.ps1` / `dotnet-test.sh`** with a high **`DOTNET_TEST_TIMEOUT_SECONDS`** (e.g. **14400**) when running the full solution or that class. For fast, managed-only NCS graph checks, use tests like **`NCSRoundtripTests`** / **`VanillaNSSCompileTests`** instead.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.
- **Test host path errors**: If you see `System.ArgumentNullException: Value cannot be null. (Parameter 'path1')` in `Path.Combine` during test execution, ensure the project is built first: `dotnet build tests/KPatcher.Tests/KPatcher.Tests.csproj` before running tests. This can occur with corrupted build state or when running tests without a prior build.

## Learned User Preferences

- When changing how mods are applied (2DA, TLK, GFF, install paths, or related patcher flows), treat mismatches with HoloPatcher or PyKotor as potential KPatcher bugs until parity with the vendored reference code is checked.
- For UI features described as matching HoloPatcher (for example install auto-detection), follow the same registry and default-path discovery approach as vendored HoloPatcher unless there is a deliberate, documented reason to differ.
- Keep the main log or status area readable at a comfortable font size, with per-line color highlighting by log level (error/warning/note) and support for normal text selection, copy, and select-all like a typical desktop text surface.
- When asked to perform work directly, use the terminal to run builds, tests, and other commands instead of only giving the user a manual step list; try the run first and fix or automate setup when it fails, rather than opening with a long prerequisite or “blocker” lecture.
- **`dotnet test`**: Use `scripts/DotnetTest.ps1` or `scripts/dotnet-test.sh` so the test process is terminated after a wall-clock limit (`DOTNET_TEST_TIMEOUT_SECONDS`, default 7200s). Avoid raw `dotnet test` in agent-driven runs.
- Tests should be meticulous and thorough with multiple assertions per stage, as strict as possible; after NCS/NSS, KCompiler, or DeNCS-related changes, run the relevant `KPatcher.Tests` coverage (including managed round-trip helpers where they apply). Prefer managed compile and decompile in those tests; do not treat registry spoofing or `nwnnsscomp.exe` as the default gate for core NSS/NCS verification.
- For repo pytest runs under `tests/py`, control log verbosity with `--kpatcher-log-level` or `--LOG_LEVEL` (and env `LOG_LEVEL`); pytest reserves `--log-level`, so do not rely on that name for KPatcher test logging.
- In PowerShell, avoid `[switch]$Parameter = $true` for on-by-default behavior; use `[bool]$Parameter = $true` instead so defaults work as intended and PSScriptAnalyzer stays clean.

### Localization

- **Supported UI languages**: English (default), Spanish, German, French, Russian, Polish. Language is chosen at startup from `CultureInfo.CurrentUICulture`; if the OS language is not in this set, the app falls back to English.
- **Resource files**: Core patcher/parity strings live in **`KPatcher.Core/Resources/PatcherResources.resx`** (and `PatcherResources.{es,de,fr,ru,pl}.resx`). UI strings live in **`KPatcher.UI/Resources/UIResources.resx`** (and `UIResources.{es,de,fr,ru,pl}.resx`). Only the default `.resx` has an associated Designer; satellite files are embedded resources.
- **TSLPatcher parity**: User-facing patcher messages that must match TSLPatcher.exe are in `PatcherResources` with English as the default; `TSLPatcherMessages` is a thin facade over `PatcherResources`. Do not change the English default text for parity-critical keys.
- **Install log**: The prefixes `"Error: "` and `"Warning: "` in `InstallLogWriter` must remain literal for KOTORModSync; only the message content is localized via `PatcherResources` at call sites.
- **Adding a language**: Add `PatcherResources.xx.resx` and `UIResources.xx.resx` with the same keys and translated values; add the two-letter code to the supported list in `App.axaml.cs` (`OnFrameworkInitializationCompleted`).
- **Localized config files**: changes.ini and namespaces.ini support language-specific variants (<basename>.<two-letter-lang>.ini or .yaml); see `docs/LOCALIZED_CONFIG_FILES.md`.

### Notes

- **KOTORModSync compatibility:** The literal prefixes `"Error: "` and `"Warning: "` in `InstallLogWriter` must remain unchanged. Only the message content should be localized.

## Learned Workspace Facts

- In `KPatcher.Core/Tools/Heuristics.cs`, skip game-detection heuristics when the candidate path contains exactly one of `swkotor.exe` (K1) or `swkotor2.exe` (K2); when both executables are present in the same path, do not skip—use the normal heuristic path.
- The DeNCS-managed port lives in `src/NCSDecomp.Core`; `src/NCSDecomp.NET` is the CLI host (`NCSDecompCLI`), not the directory that holds the bulk of the decompiler implementation. **Java→C# checklist (/lfg):** `docs/NCS_DENCS_JAVA_ACCOUNTING.md`; narrative status: `src/NCSDecomp.Core/PORTING_STATUS.md`.
- KPatcher **net9** publish is expected to ship headless tool apphosts beside the GUI (e.g. `kcompiler`, `NCSDecompCLI`) where documented in **Key commands**; the KPatcher executable’s CLI should stay aligned with HoloPatcher-style behavior, and individual tool projects should remain publishable on their own.
- Verbose developer tracing that must not land in `installlog.txt` uses `PatchLogger.AddDiagnostic` / `LogType.Diagnostic` (`ModInstaller` ignores that type for `InstallLogWriter`); in headless CLI (`Program.ExecuteCli`), each diagnostic is printed as `[DIAG]` on the console like the other subscribed log tiers.
- `KPatcher.UI/Program.cs` sends `--install`, `--uninstall`, and `--validate` straight into headless CLI; `--console` without game dir, mod path, namespace index, or one of those operations exits with code 1; the GUI path runs when `DesktopDisplayLikelyAvailable()` is true (Linux: `DISPLAY` or `WAYLAND_DISPLAY`; Windows/macOS: `Environment.UserInteractive`). With no display and no CLI indicators, it exits with a “use CLI” error.