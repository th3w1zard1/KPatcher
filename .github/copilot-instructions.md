# Copilot instructions for KPatcher

## Build, test, and lint commands

- The repo targets **.NET 9**. Install the .NET 9 SDK/runtime before running tests locally.
- Restore from the repo root with `dotnet restore KPatcher.sln`.
- Build the full solution with `dotnet build KPatcher.sln -c Debug`.
- Build the main test target the same way CI does with `dotnet build tests/KPatcher.Tests/KPatcher.Tests.csproj --no-restore -c Debug`.
- Run the analyzer-based lint pass with `dotnet build src/KPatcher.UI/KPatcher.UI.csproj --no-restore -c Release -p:RunAnalyzersDuringBuild=true`.
- Do **not** run bare `dotnet test` in automated flows. Use the repo wrappers instead:
  - Linux/macOS: `bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj --no-build -c Debug`
  - Windows/CI: `pwsh -NoProfile -File ./scripts/DotnetTest.ps1 tests/KPatcher.Tests/KPatcher.Tests.csproj --no-build -c Debug`
- Run a single test or test class by passing `--filter`, for example:
  - `bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj --no-build -c Debug --filter "FullyQualifiedName~PatcherMemoryTests"`
- `tests/KPatcher.Tests/KPatcher.Tests.csproj` uses `tests/KPatcher.Tests/Default.runsettings` by default; long suites must opt into explicit runsettings such as `tests/KPatcher.Tests/Exhaustive.runsettings`.
- Smaller satellite suites live under `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, and `tests/KEditChanges.Tests`; run them through the same timeout wrappers.

## High-level architecture

- `src/KPatcher.UI` is the main executable. `Program.cs` decides between Avalonia desktop mode and headless CLI mode (`--install`, `--uninstall`, `--validate`, `--console`) based on arguments and display availability.
- `src/KPatcher.UI/App.axaml.cs` applies language/culture selection, builds the main window, and starts the update manager. `src/KPatcher.UI/ViewModels/MainWindowViewModel.cs` owns the install workflow, namespace/game selection, progress, logs, and configuration-summary UI.
- `src/KPatcher.UI/Core.cs` is the bridge from UI/CLI into the patch engine. It normalizes mod root vs `tslpatchdata`, resolves localized `namespaces.<lang>.ini` and `changes.<lang>.ini|yaml`, validates game directories, and calls into `KPatcher.Core`.
- `src/KPatcher.Core` contains the actual patch engine. `Reader/ConfigReader.cs` parses `changes.ini` or YAML into `PatcherConfig`, `Mods/*` defines typed patch operations for InstallList/2DA/GFF/TLK/NSS/NCS/SSF, and `Patcher/ModInstaller.cs` orchestrates game detection, backup creation, install logging, required-file checks, and patch application.
- `src/KCompiler.Core` and `src/NCSDecomp.Core` are reusable managed compiler/decompiler libraries used by the patch engine and the standalone CLIs.
- `src/KCompiler.NET`, `src/NCSDecomp.NET`, and `src/KEditChanges.NET` are thin CLI fronts over those libraries. A Release publish of `src/KPatcher.UI/KPatcher.UI.csproj` also publishes bundled sidecar CLI tools via the `PublishBundledCliTools` target.
- `KCompiler.Core` links shared NCS/common source files directly from `KPatcher.Core` so the NCS model/compiler code has one source of truth without creating project-reference cycles.

## Key conventions

- Preserve the repo's **1:1 Python-port philosophy**. When behavior is unclear, check `vendor/PyKotor/Tools/KPatcher/src/kpatcher/` and translate behavior rather than redesigning it.
- The only documented intentional behavior difference from strict parity is **RTF rendering**: Avalonia tries native RTF rendering before falling back to stripped plain text.
- Stay within **C# 7.3**. Do not introduce nullable reference types, switch expressions, `using var`, null-forgiving operators, or other C# 8+ syntax.
- New tests should follow the **zero external fixture** policy from `AGENTS.md`: construct fixtures in memory, use format APIs for GFF/2DA/TLK/ERF/RIM/SSF data, and only use raw `byte[]` literals for `_corrupted` samples and `.ncs` blobs. Some legacy disk-backed corpora still exist under `tests/KPatcher.Tests`; do not add new ones.
- For installer/integration-style tests, use real temp directories and production types instead of Moq/NSubstitute. `tests/KPatcher.Tests/Policies/IntegrationFolderNoMoqTests.cs` enforces this.
- `KPatcher.Tests` references both `KPatcher.Core` and `KPatcher.UI`; prefer testing pure helpers, parsers, and format logic over full Avalonia view-model flows unless a headless UI harness is required.
- Keep config-loading changes aligned with localization. The app resolves localized `namespaces` and `changes` files from the current UI language first, then falls back to the base files.
- When touching NCS/NSS functionality, remember the split of responsibilities: `KCompiler.Core` shares linked sources from `KPatcher.Core`, while `NCSDecomp.Core` must stay self-contained and must not call external compiler executables.
- In cloud Linux sessions, Avalonia UI launch requires `DISPLAY=:1`.
