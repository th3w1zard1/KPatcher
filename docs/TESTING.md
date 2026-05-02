# Testing KPatcher

Run tests through the repo wrappers so runs cannot hang past the wall-clock cap (see [AGENTS.md](../AGENTS.md) and [.cursorrules](../.cursorrules)):

- Windows: `pwsh -NoProfile -File ./scripts/DotnetTest.ps1 tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug`
- Linux/macOS: `./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug`

Optional: leading `-TimeoutSeconds N` (capped at **600**). Exit code **124** means the wrapper killed the run; fix the bottleneck rather than skipping tests.

## Test tiers and runsettings

`tests/KPatcher.Tests/KPatcher.Tests.csproj` sets `<VSTestSetting>Default.runsettings</VSTestSetting>`.

| File | Role |
|------|------|
| `tests/KPatcher.Tests/Default.runsettings` | Default PR/local runs: excludes `DeNCSRoundTrip`, `VendorK2Game`, `TslPatcherExeGolden`, `KorExhaustiveBinaryFixtures`, `NamespaceMainAltBinaryFixtures`, `GffGitModuleTextureBinaryFixtures`, `HeadsAppearanceBinaryFixtures`. |
| `tests/KPatcher.Tests/Exhaustive.runsettings` | Only `Category=DeNCSRoundTrip` (long DeNCS/NSS harness). Example: `dotnet test ... --settings tests/KPatcher.Tests/Exhaustive.runsettings` |
| `tests/KPatcher.Tests/VendorK2Game.runsettings` | Only `Category=VendorK2Game` (requires retail-style tree via env; see test comments). |
| `tests/KPatcher.Tests/TslPatcherExeGolden.runsettings` | Only `Category=TslPatcherExeGolden` (optional `KPATCHER_TSLPATCHER_EXE`; tests no-op when unset). |
| `tests/KPatcher.Tests/KorExhaustiveBinaryFixtures.runsettings` | Kor, namespace Main/Alt, `gff_git_module_texture_bundle`, and `heads_appearance_utc_row` install rows (`KorExhaustiveBinaryFixtures` \| `NamespaceMainAltBinaryFixtures` \| `GffGitModuleTextureBinaryFixtures` \| `HeadsAppearanceBinaryFixtures`). |
| `tests/KPatcher.Tests/GeneratedRealModSmoke.runsettings` | Only `Category=GeneratedRealModInstallerSmoke` (projected real-mod installer smoke rows). |
| `tests/KPatcher.Tests/GeneratedRealModExhaustive.runsettings` | Reserved for future `Category=GeneratedRealModExhaustive` rows once exhaustive generated real-mod coverage is added. |

Override for a single run: `dotnet test --settings path/to/file.runsettings`.

### GitHub Actions tiers

- **PR / push (`ci.yml`):** `KPatcher.Tests` with default `VSTestSetting` (via `DotnetTest.ps1`), plus a **satellite smoke** job for `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, and `tests/KEditChanges.Tests`.
- **Optional (`test-optional-tiers.yml`):** `workflow_dispatch` and a **weekly schedule** run the long **DeNCS** suite (`Exhaustive.runsettings`, **without** the 600s wrapper) and **TslPatcher exe** smoke. **Vendor KotOR II** integration runs only when the workflow is dispatched with `run_vendor_k2` and repository secret `KPATCHER_K2_VENDOR_ROOT` is set. All exhaustive mod-install tests construct their payloads in memory using format builder APIs — no committed fixture files on disk. `GeneratedRealModSmoke.runsettings` is available for local or future CI runs of `GeneratedRealModInstallerSmoke`; `GeneratedRealModExhaustive.runsettings` is the reserved filter for future exhaustive generated tiers.

## No mocks in integration-style paths

- **Installer, uninstall, config on disk, format read/write:** use real temp directories, copied fixtures, and production types (`ModInstaller`, readers/writers). Do not use Moq/NSubstitute for these surfaces.
- **Helpers:** concrete test subclasses (e.g. `TestPatcherModifications`), builders (`StrictFixtureBuilder`), and small deterministic binary stubs are fine.
- **Guard:** `KPatcher.Core.Tests.Policies.IntegrationFolderNoMoqTests` fails if any `tests/KPatcher.Tests/Integration/*.cs` references Moq or `Mock<>`.

## Assertion style (formats)

- **Stable outputs you control:** prefer byte-for-byte comparison against in-memory constructed expected values when serialization is canonical.
- **GFF / ERF / RIM:** prefer semantic equality on loaded structures or round-trip `read → write → read` unless a canonical writer is documented.
- **NCS:** use `NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual` when bytecode layout may differ but the instruction graph should match.
- **Large blobs:** consider hash + length first, then a small slice around the first mismatch for logs.

## Other test projects

Smaller suites: `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, `tests/KEditChanges.Tests`. PR CI runs them in the **satellite-tests** job; run locally with the same `DotnetTest.ps1` wrapper and the project path.

## UI layer (no ModInstaller mock)

`KPatcher.UI` is referenced by `KPatcher.Tests`. Prefer tests on **pure helpers** (e.g. `KPatcher.UI.Update.RemoteUpdateInfo` JSON and channel helpers) over full **ViewModels** that require an Avalonia dispatcher unless you add a headless UI harness.

## Integration fixtures — zero external file dependencies

All test data must be defined and constructed ephemerally in `.cs` files — in memory at test time. There must be **zero committed test fixture files** on disk. The `test_files/` directory must not exist.

**Binary data exceptions:** only `_corrupted`-suffixed samples and `.ncs` (compiled NWScript bytecode) may be defined as C# `byte[]` literals. `.exe` files are omitted entirely. All other formats must be **constructed using format APIs**:
- GFF types (UTC, UTI, UTP, DLG, GIT, ARE, etc.) → `new GFF(GFFContent.UTC)` / `gff.Root.Set*()`
- 2DA → `new TwoDA(columns)` / `.AddRow()`
- TLK → `new TLK(Language.English)` / `.Add()`
- ERF/MOD → `new ERF(ERFType.MOD)` / `.SetData()`
- RIM → `new RIM()` / `.SetData()`
- SSF → `new SSF()` with slot setters
- Plaintext (INI, RTF, NSS source) → C# string constants

Prefer neutral, generic naming (no storefront- or author-specific branding).

**Preferred style for all tests:** follow `ComprehensiveIntegrationTests`, `TLKIntegrationTests`, `TwoDAAdvancedTests`, and `SSFIntegrationTests`: define `changes.ini` fragments as string literals, call `SetupIniAndConfig` / `ConfigReader`, build `TLK` / `TwoDA` / `GFF` / `SSF` in memory via format APIs, write minimal stubs to a temp `tslpatchdata` via `StrictFixtureBuilder`, call `Apply` or `ModInstaller.Install`, and assert invariants and outputs explicitly.

**Exhaustive payloads:** `ExhaustivePatternInlineInstallTests` and related test classes define all mod payloads as C# code. `ScenarioPatternModInstallHarness` runs `ModInstaller` with required synthetic game seeds. NSS script sources are string constants; NCS compiled bytecode may be `byte[]` literals.

Smaller suites: `tests/KCompiler.Tests`, `tests/NCSDecomp.Tests`, `tests/KEditChanges.Tests`. PR CI runs them in the **satellite-tests** job; run locally with the same `DotnetTest.ps1` wrapper and the project path.

## UI layer (no ModInstaller mock)

`KPatcher.UI` is referenced by `KPatcher.Tests`. Prefer tests on **pure helpers** (e.g. `KPatcher.UI.Update.RemoteUpdateInfo` JSON and channel helpers) over full **ViewModels** that require an Avalonia dispatcher unless you add a headless UI harness.

## Integration fixtures

Synthetic mod trees (`scenario_a`, `scenario_b`, `scenario_patterns`) live under `tests/KPatcher.Tests/EmbeddedIntegrationMods/` and are copied to the test output directory at build time (`KPatcher.Tests` `Content` with `CopyToOutputDirectory`). `EmbeddedIntegrationTslpatcherModTrees.GetIntegrationModsRoot()` points at that folder. Bulky archives remain under `tests/KPatcher.Tests/test_files/integration_tslpatcher_mods/`. See [INTEGRATION_TSLPATCHER_MODS.md](INTEGRATION_TSLPATCHER_MODS.md). Prefer neutral, generic naming in new fixtures (no storefront- or author-specific branding).

**Preferred style for new tests:** follow `ComprehensiveIntegrationTests`, `TLKIntegrationTests`, `TwoDAAdvancedTests`, and `SSFIntegrationTests`: define `changes.ini` fragments as string literals, call `SetupIniAndConfig` / `ConfigReader`, build or load `TLK` / `TwoDA` / `GFF` / `SSF` in memory (or write minimal stubs to a temp `tslpatchdata` via helpers), call `Apply` or `ModInstaller.Install`, and assert invariants and outputs explicitly. The copied `EmbeddedIntegrationMods/` trees are **bulk regression** over many anonymized layouts; add focused inline tests when a failure needs a tight repro.

**Inline strict patterns:** `PatternScenarioInlineMigratedTests` holds every merged InstallList→Override-only `scenario_patterns` manifest row without reading the full tree. `TslpatcherPatternModInstallerTests` keeps a single copy-from-fixture smoke on `k1_p000` plus a manifest sanity check; broader `scenario_patterns` coverage stays on `TslpatcherPatternScenarioTests` and related theories.

**Exhaustive migrated rows:** `test_files/exhaustive_pattern_inlines/` holds byte-identical `tslpatchdata` trees (every file that existed for that row) under neutral folder names; `ExhaustivePatternInlineInstallTests` runs `ModInstaller` via `ScenarioPatternModInstallHarness` plus any required synthetic game seeds (see `prepareSyntheticGame` / post-install assertions). When a payload includes `[CompileList]`, keep the shipped `.nss` inputs here so the install flow can compile them into `.ncs`; do not synthesize replacement scripts in the test harness. When you migrate another manifest row, copy its full `tslpatchdata` here, add tests, then remove the row from `scenario_patterns/manifest.json` and delete the old `EmbeddedIntegrationMods/scenario_patterns/<id>/` tree. Do not commit shipped `nwnnsscomp.exe` / patcher tool executables in these trees (KPatcher and the test compiler replace them).

**Maintainer bootstrap game roots:** fixture-refresh scripts should read from a local bootstrap tree, not from test runtime and not from a hard requirement that a full retail install be present. Real examples on the maintainer machine are `G:/SteamLibrary/Steamapps/common/swkotor` for K1 and `G:/SteamLibrary/Steamapps/common/Knights of the Old Republic II` for TSL. The important part is the on-disk shape the script needs: game exe/config files, `chitin.key`, `dialog.tlk` when required, expected `data/*.bif`, and the standard content folders. K1 uses `modules/` plus `Override/`; TSL uses `Modules/` plus `override/`. Scripts should tolerate that casing instead of assuming one spelling.

**GFF + module + texture bundle (`gff_git_module_texture_bundle`):** `changes.ini` installs `Modules\tar_m02ac.mod` and many `Override` mesh/texture sources. Install test is tagged `GffGitModuleTextureBinaryFixtures` with **no** InstallList stubs. Use `scripts/PopulateGffGitModuleTextureBundleFromK1Install.ps1 -K1GameRoot <path>` for `tar_m02ac.mod` under `tslpatchdata/Modules/`, then copy every `[install_folder1]` `Replace*` file from the packaged mod into `tslpatchdata/Override/` (names are echoed by the script).

**Kor multi-option bundle (`multi_option_kor_gff_bundle`):** install tests require real InstallList and module files under `tslpatchdata/` (no empty-template UTC in `.mod` generated in code). Omit shipped `*.exe` only. Populate retail modules with `scripts/PopulateMultiOptionKorGffBundleFromK1Install.ps1 -K1GameRoot <path>`. Tagged `KorExhaustiveBinaryFixtures`; excluded from `Default.runsettings` until committed. The `Uthar_A.ini` test (2DA-only) stays in default CI. After the full payload is in git, drop `Category!=KorExhaustiveBinaryFixtures` from `Default.runsettings` if you want those installs on every PR.

**Namespace Main/Alternate bundle (`namespace_main_alt_gff_bundle`):** `Main/changes.ini` and `Alternate/changes.ini` install from `tslpatchdata/Modules/` and (Alternate) `tslpatchdata/Override/`. Install tests are tagged `NamespaceMainAltBinaryFixtures` and use `stubMissingInstallListSources: false` — **no** synthetic minimal-GFF MOD seeding. Copy retail `.mod` files with `scripts/PopulateNamespaceMainAltGffBundleFromK1Install.ps1 -K1GameRoot <path>`; copy Alternate Override mesh/texture InstallList files from the **packaged mod** into `tslpatchdata/Override/` (see script output). Synthetic **game** `appearance.2da` rows are still written in the temp game before install so the INI’s 2DA patches have a baseline (that is game tree seeding, not mod package bytes). `ScenarioPatternModInstallHarness.CopyModsFromTslPatchDataModulesToGame` copies both `modules` and `Modules` into the temp game for Kor-style flows.

**TwoDA row patch (`twoda_feat_row_patch`):** patches `feat.2da`; the exact mod payload now lives inline in [tests/KPatcher.Tests/Integration/ExhaustivePatternInlineInstallTests.cs](../tests/KPatcher.Tests/Integration/ExhaustivePatternInlineInstallTests.cs), and the test seeds a synthetic `feat.2da` in the temp game `Override` before install (no retail `Modules` payload).

**Heads + appearance + UTC (`heads_appearance_utc_row`):** the test seeds synthetic `heads.2da` / `appearance.2da` and a minimal `helena.utc` in the **game** `Override` before install (baseline for 2DA/GFF patches). InstallList `Replace0–2` must exist under `tslpatchdata/override/` as real mod bytes: `P_Helena.tga`, `p_helenah.mdl`, `p_helenah.mdx`. Tagged `HeadsAppearanceBinaryFixtures`; excluded from default CI until committed.

**KotOR I populate scripts (maintainer bootstrap):** `scripts/PopulateExhaustivePatternInlinesFromK1Install.ps1 -K1GameRoot <path> [-Bundle All|Kor|Namespace|GffGit]` runs the per-payload scripts below. Individual entry points: `PopulateMultiOptionKorGffBundleFromK1Install.ps1`, `PopulateNamespaceMainAltGffBundleFromK1Install.ps1`, and `PopulateGffGitModuleTextureBundleFromK1Install.ps1` copy the listed `.mod` files from a KotOR I game root into the matching exhaustive inline `tslpatchdata/Modules/` (or `modules/`) trees. The maintainer game root should be a minimal bootstrap with the files and folders the script needs, not a full retail install requirement inside tests.
