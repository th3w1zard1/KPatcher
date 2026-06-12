# Testing KPatcher

Run tests through the repo wrappers so runs cannot hang past the wall-clock cap (see [AGENTS.md](../AGENTS.md) and [.cursorrules](../.cursorrules)):

- Windows: `pwsh -NoProfile -File ./scripts/DotnetTest.ps1 tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug`
- Linux/macOS: `./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug`

Optional: leading `-TimeoutSeconds N` (capped at **600**). Exit code **124** means the wrapper killed the run; fix the bottleneck rather than skipping tests.

## Test tiers and runsettings

`tests/KPatcher.Tests/KPatcher.Tests.csproj` sets `<VSTestSetting>Default.runsettings</VSTestSetting>`.

| File | Role |
|------|------|
| `tests/KPatcher.Tests/Default.runsettings` | Default PR/local runs: excludes long DeNCS, vendor, optional exe reference, reserved binary-bundle categories, and exhaustive manifest tier. |
| `tests/KPatcher.Tests/Vendor.runsettings` | Only `Category=Vendor` (vanilla NSS compile/decompile; requires populated `vendor/Vanilla_KOTOR_Script_Source`). |
| `tests/KPatcher.Tests/Exhaustive.runsettings` | Only `Category=DeNCSRoundTrip` (long DeNCS/NSS harness). |
| `tests/KPatcher.Tests/VendorK2Game.runsettings` | Only `Category=VendorK2Game` (requires retail-style tree via env; see test comments). |
| `tests/KPatcher.Tests/TslPatcherExeReference.runsettings` | Only `Category=TslPatcherExeReference` (optional `KPATCHER_TSLPATCHER_EXE`; layout smoke + manifest baseline diff). |
| `tests/KPatcher.Tests/KorExhaustiveBinaryFixtures.runsettings` | Reserved for future `KorExhaustiveBinaryFixtures` / `NamespaceMainAltBinaryFixtures` / `GffGitModuleTextureBinaryFixtures` / `HeadsAppearanceBinaryFixtures` rows (no tests registered yet). |
| `tests/KPatcher.Tests/GeneratedGenericModSmoke.runsettings` | Only `Category=GeneratedGenericModInstallerSmoke` (25 inline characterization scenarios + golden manifest oracle). |
| `tests/KPatcher.Tests/GeneratedGenericModExhaustive.runsettings` | Only `Category=GeneratedGenericModExhaustive` (`scenario_patterns/manifest.json` structural + INI-path pattern validation). Run via `scripts/validate-manifest-inventory.sh`. |

Override for a single run: `dotnet test --settings path/to/file.runsettings`.

### GitHub Actions tiers

- **PR / push (`ci.yml`):** `KPatcher.Tests` with default `VSTestSetting` (via `DotnetTest.ps1`), plus a **satellite smoke** job for `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, and `tests/KEditChanges.Tests`.
- **Optional (`test-optional-tiers.yml`):** `workflow_dispatch` and a **weekly schedule** run the long **DeNCS** suite (`Exhaustive.runsettings`, **without** the 600s wrapper), **TSLPatcher exe** smoke (`TslPatcherExeReference.runsettings`), **inline mod smoke** (`GeneratedGenericModSmoke.runsettings`), and **manifest inventory** (`GeneratedGenericModExhaustive.runsettings`). **Vanilla NSS** (`Vendor.runsettings`) runs when dispatched with `run_vendor_nss`. **Vendor KotOR II** integration runs only when dispatched with `run_vendor_k2` and repository secret `KPATCHER_K2_VENDOR_ROOT` is set.

## No mocks in integration-style paths

- **Installer, uninstall, config on disk, format read/write:** use real temp directories and production types (`ModInstaller`, readers/writers). Do not use Moq/NSubstitute for these surfaces.
- **Helpers:** concrete test subclasses, builders (`StrictFixtureBuilder`), and small deterministic binary stubs are fine.
- **Guard:** `KPatcher.Core.Tests.Policies.IntegrationFolderNoMoqTests` fails if a legacy `tests/KPatcher.Tests/Integration/` tree reappears with Moq usage.

## Install-path test portfolio

| Family | Purpose | Representative tests / helpers |
|--------|---------|--------------------------------|
| **Characterization** | KPatcher install behavior on synthetic mods built in memory | `Patcher/*IntegrationTests.cs`, `EmbeddedScenarioPatternInstallTests` (`Category=GeneratedGenericModInstallerSmoke`) |
| **Contract** | INI serializer ↔ `ConfigReader` round-trip | `Mods/KPatcherINISerializer*Tests.cs` |
| **Oracle** | Game-tree manifest fingerprints, CLI vs direct install diff, optional exe/baseline tiers | `ModInstallerOracleReferenceTests`, `ModInstallerCliOracleTests`, `TslPatcherOracleHarness` |

Shared harness: `ModInstallerIntegrationEnvironment`, `ModInstallerIntegrationTestBase`, `InstallAssertionLadder`, `InstallManifestSnapshot`, `PipelineOrderFixtures`, `ScenarioGoldenManifests`, `TslPatcherOracleHarness`, `ManifestIniPathPatternRegistry`.

Twenty-five inline scenarios in `EmbeddedScenarioDefinitions` assert golden SHA-256 manifests (`Category=GeneratedGenericModInstallerSmoke`). Refresh goldens via `KP_CAPTURE_SCENARIO_GOLDENS=1` and `scripts/compute-inline-scenario-fingerprints.sh`. Export a single-scenario baseline for manual TSLPatcher comparison via `scripts/export-kpatcher-oracle-baseline.sh`. Validate `manifest.json` via `scripts/validate-manifest-inventory.sh`.

`EmbeddedIntegrationMods/scenario_patterns/manifest.json` inventories **116** legacy mod layout ids (`k1_p*` / `tsl_p*`) for maintainer reference. Inline characterization ids are **disjoint** from manifest ids. `ManifestIniPathPatternRegistry` classifies every manifest row’s `ChangesIniRelative` into four path-shape classes (`root_changes_ini`, `subfolder_changes_ini`, `variant_changes_ini`, `custom_ini_name`) and maps each class to inline scenarios — pattern coverage without committed mod byte trees. `ManifestScenarioCoverageRegistry.PipelineStageInlineScenarios` maps install pipeline stages to inline scenarios.

CLI oracle (`ModInstallerCliOracleTests`) covers all install scenarios except those that require a non-default INI path without `namespaces.ini` (`inline_settings_only`, `inline_custom_ini_name`, `inline_variant_ini_filename`).

Optional oracle env vars (opt-in tiers, not default CI):

| Variable | Purpose |
|----------|---------|
| `KPATCHER_TSLPATCHER_EXE` | Path to TSLPatcher.exe for layout/smoke tier. GUI-only — no headless install. |
| `KPATCHER_ORACLE_MANIFEST_BASELINE` | Text file of `path\|length\|sha256` lines from a manual TSLPatcher install for diff against KPatcher. |

`ModInstaller` resolves INI paths with `SystemHelpers.CombineUnderRoot` so Windows-style `Modules\file.mod` entries work on Linux CI.

## Assertion style (formats)

- **Stable outputs you control:** prefer byte-for-byte comparison against in-memory constructed expected values when serialization is canonical.
- **GFF / ERF / RIM:** prefer semantic equality on loaded structures or round-trip `read → write → read` unless a canonical writer is documented.
- **NCS:** use `NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual` when bytecode layout may differ but the instruction graph should match.
- **Large blobs:** consider hash + length first, then a small slice around the first mismatch for logs.

## Integration fixtures — zero external file dependencies

All test data must be defined and constructed ephemerally in `.cs` files — in memory at test time. There must be **zero committed test fixture files** on disk. The `test_files/` directory must not exist.

**Binary data exceptions:** only `_corrupted`-suffixed samples and `.ncs` (compiled NWScript bytecode) may be defined as C# `byte[]` literals. `.exe` files are omitted entirely. All other formats must be **constructed using format APIs** (`new GFF(...)`, `new TwoDA(...)`, `new TLK(...)`, `new ERF(...)`, etc.) or plaintext string constants.

**Preferred style:** define `changes.ini` fragments as string literals, build formats in memory, use `ModInstallerIntegrationEnvironment` / `StrictFixtureBuilder` for temp trees, call `ModInstaller.Install` or format `Apply`, and assert explicitly. See `ComprehensiveIntegrationTests`, `TLKIntegrationTests`, `TwoDAAdvancedTests`, and `SSFIntegrationTests`.

The only copied artifact under `EmbeddedIntegrationMods/` is `scenario_patterns/manifest.json` (metadata inventory, not installable mod trees). The legacy `tests/KPatcher.Tests/Integration/` corpus was removed. Maintainer scripts under `scripts/` (for example `GenerateTslpatcherPatternScenarios.ps1`, `Populate*FromK1Install.ps1`) support offline analysis and optional local bootstrap — they are **not** consumed by default CI.

See [INTEGRATION_TSLPATCHER_MODS.md](INTEGRATION_TSLPATCHER_MODS.md) for maintainer download/inventory workflows.

## Other test projects

Smaller suites: `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, `tests/KEditChanges.Tests`. PR CI runs them in the **satellite-tests** job.

## UI layer (no ModInstaller mock)

`KPatcher.UI` is referenced by `KPatcher.Tests`. Prefer tests on **pure helpers** (e.g. `KPatcher.UI.Update.RemoteUpdateInfo`) over full **ViewModels** that require an Avalonia dispatcher unless you add a headless UI harness.
