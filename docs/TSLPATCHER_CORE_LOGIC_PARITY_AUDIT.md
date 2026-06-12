---
title: "TSLPatcher Core Logic Parity Audit"
status: active
date: 2026-06-11
---

# TSLPatcher Core Logic Parity Audit

## Observation boundary

- [REPO] This pass compares KPatcher's behavior-owning install/config surfaces against repo-local parity documents and the locally checked-out TSLPatcher Delphi source tree under `vendor/TSLPatcher`.
- [REPO] The reviewed KPatcher surfaces now include install orchestration, namespace/config parsing, TLK handling, 2DA row-value handling, GFF `!FieldPath` handling, HACK/NCS parsing and patching, NSS compilation flow, SSF writes, override handling, and uninstall/backup logic.
- [REPO] The reviewed TSLPatcher surfaces now include the current `UTSLPatcher.pas`, the older `UTSLPatcher12.pas`, the repo-local build-verification doc, and the previously inspected namespace behavior from the Delphi UI units.
- [OPEN] This audit still does not claim byte-for-byte equivalence for every helper routine in every Delphi unit. It promotes only discrepancies or alignments that were directly observed in the reviewed behavior-owning paths.

## Confirmed findings

### 1. Pipeline order — resolved (binary-verified target)

- [REPO] The older Delphi snapshot (`UTSLPatcher12.pas`) runs `TLK -> 2DA -> GFF -> HACK -> Compile -> InstallList` (historical only).
- [REPO] The newer Delphi snapshot (`UTSLPatcher.pas`) runs `TLK -> InstallList -> 2DA -> GFF -> HACK -> Compile -> SSF` (reconstructed source; differs from shipped binary).
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](TSLPATCHER_BUILD_VERIFICATION.md) documents the Ghidra-confirmed shipped binary order: `TLK -> GFF -> 2DA -> InstallList -> HACK -> NSS -> SSF`.
- [REPO] **Parity target chosen:** binary-verified order (not newer Delphi source order).
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) queues `TLK -> GFF -> 2DA -> InstallList -> NCS (HACK) -> NSS (Compile) -> SSF`, matching the binary-verified sequence.
- [SYNTH] Pipeline ordering is aligned for core install parity. Remaining drift is limited to intentional HACK/Compile implementations (NCS-only HACK, managed compile) documented below.

### 2. Namespace fallback and path confinement — mostly aligned (2026-06-10)

- [REPO] TSLPatcher falls back to root `changes.ini` / `info.rtf` when namespace-specific files are missing, and rejects `DataPath` values containing `..\`.
- [REPO] [src/KPatcher.Core/Reader/NamespaceReader.cs](src/KPatcher.Core/Reader/NamespaceReader.cs) now defaults blank `IniName`/`InfoName` to `changes.ini`/`info.rtf` and clears `DataPath` segments containing `..`.
- [REPO] [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) applies the same fallback and localized resolution at **install** time via `ResolveInstallPaths` (preview and install paths now match).
- [SYNTH] Namespace parity is largely landed; remaining gap is namespace selection keyed by display `Name` rather than section id (documented extension).

### 3. InstallList overwrite safety — resolved

- [REPO] The newer Delphi source blocks `Replace#` installs from overwriting existing `.exe`, `.tlk`, `.key`, and `.bif` targets in the game folder.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) implements `ShouldSkipProtectedInstallListOverwrite` for folder-level `InstallFile` replace operations on those extensions.
- [REPO] Capsule destinations still allow replace (matches TSLPatcher capsule behavior); characterization tests in `ModInstallerTests.ShouldPatch_InstallFileReplaceExisting_ProtectedFolderTargets_ShouldSkip`.
- [SYNTH] InstallList overwrite safeguards now match historical TSLPatcher folder protections.

### 4. KPatcher narrows TSLPatcher's generic HACKList behavior to NCS-only patching

- [REPO] The current Delphi `UpdateHackFiles()` / `DoFileHack()` flow opens arbitrary selected files and applies offset/value writes directly to those files.
- [REPO] [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) loads `[HACKList]` entries exclusively into `ModificationsNCS` objects, not a generic binary patch handler.
- [REPO] [src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs](src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs) only supports NCS-oriented token/value writes and explicitly rejects `!FieldPath`-shaped `2DAMEMORY` values in `[HACKList]` patches.
- [SYNTH] KPatcher covers the script-bytecode patching use case, but it is not strict parity with TSLPatcher's generic binary HACKList semantics.

### 5. CompileList orchestration — partial (intentional managed compile)

- [REPO] The current Delphi `DoCompileFiles()` flow writes processed NSS files, shells `nwnnsscomp.exe`, honors `ScriptCompilerFlags`, optionally keeps processed sources via `SaveProcessedScripts`, and routes compiled output into override or ERF/RIM destinations.
- [REPO] [src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs](src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs) replaces tokens in managed code and compiles via `KCompiler` (`NCSAuto.CompileNss(...)`).
- [REPO] [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) loads `ScriptCompilerFlags` from `[Settings]` and propagates it to `ModificationsNSS`; tests in `ConfigReaderCompileListTests` and `ModificationsNSSTests`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) honors `SaveProcessedScripts` for temp-script cleanup.
- [SYNTH] Settings and tokenized compile orchestration align; the **compiler backend** is an intentional product choice (managed `KCompiler`, no `nwnnsscomp.exe` in product paths per repo policy).

### 6. 2DA modifier apply order aligned (2026-06-10)

- [REPO] TSLPatcher applies `[2DAList]` modifiers in `changes.ini` section order (`UTSLPatcher.pas` lines 3067–3091).
- [REPO] [src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs](src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs) now iterates `Modifiers` in load order instead of grouping by modifier type.
- [SYNTH] Interleaved AddColumn/ChangeRow INIs now match TSLPatcher sequencing; characterization tests in `TwoDaModifierOrderTests`.

### 7. K1 2DA hardcaps — removed (former drift closed)

- [REPO] A prior KPatcher build enforced K1-only row limits for `placeables.2da`, `upcrystals.2da`, and `upgrade.2da`; no matching hardcap exists in reviewed TSLPatcher Delphi sources.
- [REPO] Hardcaps were **removed** from [src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs](src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs); `PatchResource_K1VendorParityFiles_ShouldStillApplyChangesBeyondFormerHardcaps` guards against regression.
- [SYNTH] KPatcher no longer applies vendor-incompatible row truncation on those 2DAs.

### 8. Backup and uninstall semantics differ materially

- [REPO] The Delphi patch handler stores single-copy backups under the patcher application's `backup\` folder.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) creates timestamped `backup/<timestamp>` folders under the mod tree and clears a sibling `uninstall` directory before creating the new backup.
- [REPO] [src/KPatcher.Core/Uninstall/ModUninstaller.cs](src/KPatcher.Core/Uninstall/ModUninstaller.cs) and [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) expose a concrete uninstall workflow that restores the newest timestamped backup.
- [SYNTH] KPatcher intentionally extends backup behavior to support uninstall, but that is not strict core-behavior parity and should not be summarized as "same as TSLPatcher."
- [OPEN] Keep this documented as an intentional extension unless the product decides to trade uninstall safety for stricter historical behavior.

### 9. Documentation and source-vs-runtime drift

- [REPO] The 2026-06-10 parity iteration (PR #18, merged to `master`) closed confirmed core-logic gaps: install paths, TLK append dedup, 2DA INI order, `SafeStrToInt`, ResRef INI sanitization, writable clearing, settings CRLF, pipeline order, InstallList guards, ScriptCompilerFlags, K1 hardcap removal.
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](TSLPATCHER_BUILD_VERIFICATION.md) references `docs/TSLPATCHER_RE.md`, which is **not** present in the tree; the pipeline table in the build-verification doc remains the authoritative binary RE summary until that doc is restored.
- [REPO] Reconstructed Delphi source (`UTSLPatcher.pas`) still disagrees with the binary-verified order; KPatcher explicitly targets the binary.
- [SYNTH] Parity confidence is **moderate-to-strong for core install behavior**, with documented intentional non-parity (generic HACK, external compiler, backup/uninstall, namespace display-name selection).

## Resolved non-gaps from the broader pass

- [REPO] TLK append/token support exists on both sides. The Delphi `ProcessTLKData()` / `AppendTLKData()` path and [src/KPatcher.Core/Mods/TLK/ModificationsTLK.cs](src/KPatcher.Core/Mods/TLK/ModificationsTLK.cs) both support TLK token mapping, append-file overrides, dialog append targets, memory-backed StrRef substitution, and **append deduplication** (reuse existing dialog entries when text+sound match — landed 2026-06-10).
- [REPO] KPatcher does implement `!FieldPath` and `2DAMEMORY` path indirection. [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) parses `2DAMEMORY#=!FieldPath`, and [src/KPatcher.Core/Mods/GFF/ModifyGFF.cs](src/KPatcher.Core/Mods/GFF/ModifyGFF.cs) stores and dereferences those paths through `Memory2DAModifierGFF`.
- [REPO] ERF/RIM override handling exists on both sides. The Delphi `HandleERFOverrideType(...)` logic and [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) both support ignore/warn/rename for override-folder shadowing when destination is **not** `Override` (see §10 destination guard); KPatcher additionally warns when a `.mod` shadows a RIM/ERF destination.
- [REPO] KPatcher does implement `high()` row-value support. [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) parses `high()` into `RowValueHigh`, and [src/KPatcher.Core/Mods/TwoDA/RowValue.cs](src/KPatcher.Core/Mods/TwoDA/RowValue.cs) resolves the next numeric row label or column value using a max-plus-one rule that matches the reviewed older Delphi behavior.
- [REPO] Both codebases expose the same major patch families: TLK, 2DA, GFF, InstallList, HACK/NCS, Compile/NSS, and SSF.
- [SYNTH] The broader pass removed several earlier uncertainties. The confirmed gaps are narrower and more specific than "whole handler missing," but they are still real core-logic differences.

### 10. Settings and modifier gaps closed (2026-06-11, `feat/tslpatcher-parity-gap-close`)

- [REPO] **InstallerMode** — TSLPatcher `DoInstallFiles` skips `[InstallList]` when `[Settings] InstallerMode` is false (default). [src/KPatcher.Core/Config/PatcherConfig.cs](src/KPatcher.Core/Config/PatcherConfig.cs) and [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) now gate InstallList queuing; `ParseIniBool` accepts `1`/`0` and `true`/`false`.
- [REPO] **BackupFiles** — TSLPatcher `l_dlgopen.DoBackups` disables backup creation when false. `PatcherConfig.BackupFiles` (default true) gates `GetBackup` / `CreateBackupHelper`.
- [REPO] **PlaintextLog** — `[Settings] PlaintextLog` selects `installlog.txt` vs `installlog.rtf` via `ModInstaller.EnsureInstallLogWriter` (`useRtf: !cfg.PlaintextLog`).
- [REPO] **2DA `inc(n)`** — Delphi `UTSLPatcher.pas` `inc()` row labels; `RowValueInc` in [src/KPatcher.Core/Mods/TwoDA/RowValue.cs](src/KPatcher.Core/Mods/TwoDA/RowValue.cs), parsed in `ConfigReader.Cells2DA`.
- [REPO] **2DA modifier key matching** — `ConfigReader.Matches2DAModifierKey` uses `IndexOf` (Delphi `Pos`) instead of `StartsWith` for embedded modifier keys.
- [REPO] **Exclusive-column fallback** — `Modify2DA.UnpackExclusiveFallback` skips `RowValueInc` / `RowValueHigh` when updating existing exclusive-column rows (AddRow/CopyRow parity).
- [REPO] **GFF field key `2DAMEMORY#`** — `PatcherMemory.ResolveMemoryToken` resolves memory tokens in GFF field keys at apply time ([src/KPatcher.Core/Mods/GFF/ModifyGFF.cs](src/KPatcher.Core/Mods/GFF/ModifyGFF.cs)).
- [REPO] **!OverrideType destination guard** — `HandleOverrideType` skips when destination is `Override` (matches Delphi `HandleERFOverrideType` early exit); integration tests in `ModInstallerOverrideTypeTests` use capsule destinations.
- [SYNTH] These were confirmed omissions from the 2026-06-10 iteration; they do not change intentional non-parity items (generic HACK, managed compile, backup/uninstall extension).

## Recommended follow-up slices

1. **Product decision (optional):** Grow generic binary-offset `[HACKList]` parity beyond NCS-only patching — large scope; current behavior is intentional.
2. **Product decision (optional):** External `nwnnsscomp.exe` compile parity — rejected by repo policy; managed `KCompiler` is the product path.
3. **Low priority:** Trace `UStrTok.pas` callers in format units; not referenced from `.dpr`; document-only unless a mod corpus needs tokenizer parity.
4. **Harness:** Golden/interleaved 2DA INI corpora if regressions appear in the wild.
5. **Docs hygiene:** Restore or replace missing `docs/TSLPATCHER_RE.md` for full Ghidra function tables linked from build verification.

### 11. Pipeline test and HACKList export (2026-06-12, `feat/parity-pipeline-test-and-hack-serialize`)

- [REPO] `ModInstallerPipelineOrderIntegrationTests` asserts binary-verified install queue order via `PatchLogger` diagnostics (`TLK → GFF → 2DA → InstallList → NCS → NSS → SSF`).
- [REPO] `KPatcherINISerializer.SerializeHackList` round-trips `[HACKList]` entries (`KPatcherINISerializerHackListTests`).
- [REPO] `ModInstaller` ctor defaults install log to RTF (`installlog.rtf`) matching `PlaintextLog=false` before `EnsureInstallLogWriter` runs.

### 12. Cross-stage memory and settings integration coverage (2026-06-12)

- [REPO] `ModInstallerCrossStageMemoryIntegrationTests` verifies `[2DAList]` memory tokens flow to a later `[SSFList]` patch in one install (2DA runs before SSF in binary-verified order).
- [REPO] Settings integration tests cover `BackupFiles=true` backup creation, `SaveProcessedScripts` temp-folder retention/cleanup, and `!OverrideType=rename` module install behavior.
- [REPO] `KPatcherINISerializer.SerializeCompileList` round-trips `[CompileList]` through `ConfigReader`.
- [SYNTH] GFF field-key `2DAMEMORY#` resolution remains unit-tested only — GFF runs before 2DA in the binary-verified pipeline, so 2DA-populated field keys cannot affect GFF at install time.
