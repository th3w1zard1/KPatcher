# TSLPatcher Binary Reverse-Engineering Reference

Authoritative index for TSLPatcher.exe behavior recovered from Ghidra analysis and how it maps to KPatcher. For vendor Pascal source layout, see [vendor/TSLPatcher/README.md](../vendor/TSLPatcher/README.md). For build verification of the vendor tree, see [TSLPATCHER_BUILD_VERIFICATION.md](TSLPATCHER_BUILD_VERIFICATION.md).

## Parity target

KPatcher targets the **shipped TSLPatcher.exe binary** (Ghidra-confirmed), not necessarily the newest checked-in Delphi source order in `UTSLPatcher.pas`. When Pascal snapshots disagree with the binary, the binary wins. Full behavioral comparison notes live in [TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md](TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md).

## Binary-confirmed install pipeline

From `Execute_Pipeline` @ `0x0047eec8` (see [TSLPATCHER_BUILD_VERIFICATION.md](TSLPATCHER_BUILD_VERIFICATION.md)):

| Step | Function (address) | INI section | KPatcher owner |
|------|-------------------|-------------|----------------|
| 0 | `CountModifications` @ `0x0047ebf8` | (progress pre-pass) | `ModInstaller.Install` patch counting |
| 1 | `PatchTLK` @ `0x00479850` | `[TLKList]` | `ModificationsTLK`, `ConfigReader` TLK |
| 2 | `PatchGFF` @ `0x0048290c` | `[GFFList]` | `ModificationsGFF`, `ModifyGFF` |
| 3 | 2DA dispatch loop | `[2DAList]` | `Modifications2DA`, `Modify2DA` |
| 4 | `ProcessInstallList` @ `0x0047c280` | `[InstallList]` | `InstallFile`, `ModInstaller` |
| 5 | `ProcessHACKList` @ `0x00482150` | `[HACKList]` | `ModificationsNCS` (NCS-only narrowing) |
| 6 | `CompileNSS` @ `0x0047d4ec` | `[CompileList]` | `ModificationsNSS`, managed `KCompiler` |
| 7 | `PatchSSF` @ `0x0047e514` | `[SSFList]` | `ModificationsSSF` |

KPatcher queues the same sequence in [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs). Characterization: `ModInstallerPipelineOrderIntegrationTests`.

## Vendor Pascal unit → KPatcher mapping

| TSLPatcher unit | Role | KPatcher equivalent |
|-----------------|------|---------------------|
| `UTSLPatcher.pas` | Install orchestration, settings, logging | `ModInstaller`, `PatcherConfig` |
| `UST_IniFile.pas` | INI load, `<#LF#>` tokens | `ConfigReader`, `ConfigReaderIni` |
| `UMainForm.pas` / `UNamespaceForm.pas` | Namespace UI selection | `NamespaceReader`, `KPatcher.UI/Core.cs` |
| `U2DAEdit.pas` | 2DA tables | `Mods/TwoDA/*`, `Formats/TwoDA/*` |
| `UTLKFile.pas` | TLK V3 | `Mods/TLK/*`, `Formats/TLK/*` |
| `UGFFFile.pas` | GFF V3.2 | `Mods/GFF/*`, `Formats/GFF/*` |
| `UERFHandler.pas` | ERF/RIM/MOD | `Formats/ERF/*`, `Common/Capsule` |
| `USSFFile.pas` | SSF sounds | `Mods/SSF/*`, `Formats/SSF/*` |
| `UST_Common.pas` | File utilities, backups | `SystemHelpers`, backup helpers in `ModInstaller` |

## Install-path characterization (KPatcher.Tests)

| Layer | Tests / helpers |
|-------|----------------|
| Inline scenarios (20) | `EmbeddedScenarioDefinitions`, `EmbeddedScenarioPatternInstallTests` |
| Golden manifests | `ScenarioGoldenManifests`, `InstallManifestSnapshot` |
| Oracle | `TslPatcherOracleHarness`, `ModInstallerCliOracleTests`, `ModInstallerOracleReferenceTests` |
| Stage coverage map | `ManifestScenarioCoverageRegistry.PipelineStageInlineScenarios` |
| Legacy manifest inventory | `EmbeddedIntegrationMods/scenario_patterns/manifest.json` (116 rows, migration pending) |

Refresh goldens: `KP_CAPTURE_SCENARIO_GOLDENS=1` and `scripts/compute-inline-scenario-fingerprints.sh`. Export baseline: `scripts/export-kpatcher-oracle-baseline.sh`.

## Intentional non-parity (product choices)

Documented in the parity audit; do not treat as harness gaps:

- **Generic `[HACKList]`** — TSLPatcher patches arbitrary files by offset; KPatcher implements NCS bytecode hacks only.
- **External compiler** — TSLPatcher shells `nwnnsscomp.exe`; KPatcher uses managed `KCompiler` (repo policy).
- **Backup/uninstall** — KPatcher timestamped mod-tree backups and uninstall restore (extension beyond TSLPatcher app-local backups).
- **Namespace UI** — KPatcher may select namespace by display `Name` in addition to section id.
- **LZMA / `.bzf`** — `LzmaHelper` is a placeholder; iOS `.bzf` chitin paths are not decompressed yet ([src/KPatcher.Core/Common/LZMA/LzmaHelper.cs](src/KPatcher.Core/Common/LZMA/LzmaHelper.cs)).

## CompileNSS and nwnnsscomp.exe

TSLPatcher step 6 (`CompileNSS` @ `0x0047d4ec`) shells BioWare's `nwnnsscomp.exe`. KPatcher replaces that path with managed `KCompiler` (see [NWNNSSCOMP_RE.md](NWNNSSCOMP_RE.md) and `NwnnsscompReMapping.cs`). `ScriptCompilerFlags` from `[Settings]` is honored; external compiler execution is intentionally not used in product builds.

## Related documents

- [TSLPATCHER_BUILD_VERIFICATION.md](TSLPATCHER_BUILD_VERIFICATION.md) — vendor tree compile verification
- [TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md](TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md) — evidence-labeled gap analysis
- [PARITY_CONFIDENCE_LEDGER.md](PARITY_CONFIDENCE_LEDGER.md) — strategy-level confidence summary
- [TESTING.md](TESTING.md) — test tiers and install-path portfolio
- [NWNNSSCOMP_RE.md](NWNNSSCOMP_RE.md) — legacy external compiler RE (superseded by KCompiler for product paths)
