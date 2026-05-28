---
title: "TSLPatcher Core Logic Parity Audit"
status: active
date: 2026-05-28
---

# TSLPatcher Core Logic Parity Audit

## Observation boundary

- [REPO] This pass compares KPatcher's behavior-owning install/config surfaces against repo-local parity documents and the locally checked-out TSLPatcher Delphi source tree under `vendor/TSLPatcher`.
- [REPO] The reviewed KPatcher surfaces now include install orchestration, namespace/config parsing, TLK handling, 2DA row-value handling, GFF `!FieldPath` handling, HACK/NCS parsing and patching, NSS compilation flow, SSF writes, override handling, and uninstall/backup logic.
- [REPO] The reviewed TSLPatcher surfaces now include the current `UTSLPatcher.pas`, the older `UTSLPatcher12.pas`, the repo-local build-verification doc, and the previously inspected namespace behavior from the Delphi UI units.
- [OPEN] This audit still does not claim byte-for-byte equivalence for every helper routine in every Delphi unit. It promotes only discrepancies or alignments that were directly observed in the reviewed behavior-owning paths.

## Confirmed findings

### 1. Pipeline truth is split across repo sources, and KPatcher does not fully match any of them

- [REPO] The older Delphi snapshot (`UTSLPatcher12.pas`) runs `TLK -> 2DA -> GFF -> HACK -> Compile -> InstallList`.
- [REPO] The newer Delphi snapshot (`UTSLPatcher.pas`) runs `TLK -> InstallList -> 2DA -> GFF -> HACK -> Compile -> SSF`.
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md) states the verified shipped binary runs `TLK -> GFF -> 2DA -> InstallList -> HACK -> NSS -> SSF`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) currently queues `TLK -> InstallList -> 2DA -> GFF -> NSS -> NCS -> SSF` and comments that this is the TSLPatcher order.
- [SYNTH] KPatcher does not currently match the binary-verified order, and the repo cannot honestly describe one single authoritative TSLPatcher pipeline without first deciding whether parity targets the verified binary or the reconstructed WIP Delphi source.
- [OPEN] A code-fix pass should not change patch ordering until the parity target is chosen explicitly.

### 2. Namespace fallback and path confinement diverge

- [REPO] The original namespace UI falls back to `changes.ini` / `info.rtf` when a namespace entry omits or mispoints those files, and it rejects `DataPath` values containing `..\` so a namespace cannot escape `tslpatchdata`.
- [REPO] [src/KPatcher.Core/Namespaces/PatcherNamespace.cs](src/KPatcher.Core/Namespaces/PatcherNamespace.cs) still defines defaults for `changes.ini` and `info.rtf`, but [src/KPatcher.Core/Reader/NamespaceReader.cs](src/KPatcher.Core/Reader/NamespaceReader.cs) treats `IniName` and `InfoName` as required and throws if they are absent.
- [REPO] [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) resolves `DataFolderPath` through `Path.Combine` and localized-file resolution without an equivalent `..\` confinement guard.
- [SYNTH] KPatcher is stricter than TSLPatcher when namespace files are missing, but more permissive about namespace paths escaping `tslpatchdata`. Both are real behavior differences.
- [OPEN] If parity with original namespace behavior matters, KPatcher needs both a fallback decision and a path-confinement rule.

### 3. InstallList overwrite safety checks are missing on the KPatcher side

- [REPO] The newer Delphi source blocks `Replace#` installs from overwriting existing `.exe`, `.tlk`, `.key`, and `.bif` targets.
- [REPO] [src/KPatcher.Core/Mods/InstallFile.cs](src/KPatcher.Core/Mods/InstallFile.cs) is a passthrough copy operation, and the reviewed KPatcher install path only special-cases direct TLK patch writes to `dialog.tlk` in [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs).
- [REPO] No equivalent InstallList extension filter was found in the reviewed KPatcher C# path.
- [SYNTH] This is a confirmed parity and safety gap. KPatcher can presently replace targets that classic TSLPatcher explicitly refused to overwrite.
- [OPEN] Add a focused fix slice after the pipeline target is settled.

### 4. KPatcher narrows TSLPatcher's generic HACKList behavior to NCS-only patching

- [REPO] The current Delphi `UpdateHackFiles()` / `DoFileHack()` flow opens arbitrary selected files and applies offset/value writes directly to those files.
- [REPO] [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) loads `[HACKList]` entries exclusively into `ModificationsNCS` objects, not a generic binary patch handler.
- [REPO] [src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs](src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs) only supports NCS-oriented token/value writes and explicitly rejects `!FieldPath`-shaped `2DAMEMORY` values in `[HACKList]` patches.
- [SYNTH] KPatcher covers the script-bytecode patching use case, but it is not strict parity with TSLPatcher's generic binary HACKList semantics.

### 5. CompileList orchestration differs even though both sides support tokenized NSS compilation

- [REPO] The current Delphi `DoCompileFiles()` flow writes processed NSS files, shells `nwnnsscomp.exe`, honors `ScriptCompilerFlags`, optionally keeps processed sources via `SaveProcessedScripts`, and then routes compiled output into override or ERF/RIM destinations.
- [REPO] [src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs](src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs) replaces tokens in managed code and compiles via `NCSAuto.CompileNss(...)`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) still honors `SaveProcessedScripts` for temp-script cleanup behavior, but no equivalent `ScriptCompilerFlags` setting was found in the reviewed C# tree.
- [SYNTH] This is not just an organizational cleanup. KPatcher intentionally replaces the external-compiler workflow with an in-process compile path, and some configuration surface differs.

### 6. K1 2DA hardcaps are a KPatcher-specific rule

- [REPO] [src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs](src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs) enforces K1-only row limits for `placeables.2da`, `upcrystals.2da`, and `upgrade.2da`.
- [REPO] No matching hardcap check was found in the reviewed TSLPatcher Delphi sources during this pass.
- [SYNTH] This looks like a KPatcher-specific safety or compatibility addition rather than inherited TSLPatcher logic.

### 7. Backup and uninstall semantics differ materially

- [REPO] The Delphi patch handler stores single-copy backups under the patcher application's `backup\` folder.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) creates timestamped `backup/<timestamp>` folders under the mod tree and clears a sibling `uninstall` directory before creating the new backup.
- [REPO] [src/KPatcher.Core/Uninstall/ModUninstaller.cs](src/KPatcher.Core/Uninstall/ModUninstaller.cs) and [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) expose a concrete uninstall workflow that restores the newest timestamped backup.
- [SYNTH] KPatcher intentionally extends backup behavior to support uninstall, but that is not strict core-behavior parity and should not be summarized as "same as TSLPatcher."
- [OPEN] Keep this documented as an intentional extension unless the product decides to trade uninstall safety for stricter historical behavior.

### 8. The repo's parity documentation is overstated relative to the available evidence

- [REPO] Earlier repo parity summaries overstated confidence before this refresh pass; the refreshed ledger now points back to this audit and carries the downgraded status.
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md) links `docs/TSLPATCHER_RE.md`, which is not present in the current tree.
- [REPO] The build-verification document's pipeline claim conflicts with the currently reviewed Delphi source snapshots.
- [SYNTH] The repo's parity story had been stronger than the evidence supported. This audit and the refreshed ledger now align on the downgraded confidence level, but the underlying source-vs-runtime drift remains unresolved.

## Resolved non-gaps from the broader pass

- [REPO] TLK append/token support exists on both sides. The Delphi `ProcessTLKData()` / `AppendTLKData()` path and [src/KPatcher.Core/Mods/TLK/ModificationsTLK.cs](src/KPatcher.Core/Mods/TLK/ModificationsTLK.cs) both support TLK token mapping, append-file overrides, dialog append targets, and memory-backed StrRef substitution.
- [REPO] KPatcher does implement `!FieldPath` and `2DAMEMORY` path indirection. [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) parses `2DAMEMORY#=!FieldPath`, and [src/KPatcher.Core/Mods/GFF/ModifyGFF.cs](src/KPatcher.Core/Mods/GFF/ModifyGFF.cs) stores and dereferences those paths through `Memory2DAModifierGFF`.
- [REPO] ERF/RIM override handling exists on both sides. The Delphi `HandleERFOverrideType(...)` logic and [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) both support ignore/warn/rename handling for override-folder shadowing, and KPatcher additionally warns when a `.mod` shadows a RIM/ERF destination.
- [REPO] KPatcher does implement `high()` row-value support. [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) parses `high()` into `RowValueHigh`, and [src/KPatcher.Core/Mods/TwoDA/RowValue.cs](src/KPatcher.Core/Mods/TwoDA/RowValue.cs) resolves the next numeric row label or column value using a max-plus-one rule that matches the reviewed older Delphi behavior.
- [REPO] Both codebases expose the same major patch families: TLK, 2DA, GFF, InstallList, HACK/NCS, Compile/NSS, and SSF.
- [SYNTH] The broader pass removed several earlier uncertainties. The confirmed gaps are narrower and more specific than "whole handler missing," but they are still real core-logic differences.

## Recommended follow-up slices

1. Choose the authoritative parity target for pipeline ordering: verified binary behavior or current reconstructed Delphi source.
2. Align KPatcher's InstallList overwrite safeguards with the historical `.exe` / `.tlk` / `.key` / `.bif` protections.
3. Decide whether KPatcher should preserve its NCS-only HACKList narrowing or grow a generic binary-offset patch surface for strict TSLPatcher parity.
4. Decide whether KPatcher should adopt TSLPatcher-style namespace fallbacks and `DataPath` confinement.
5. Decide whether CompileList needs closer parity for external-compiler settings such as `ScriptCompilerFlags`.
6. Confirm whether the K1 hardcap rules are intentional product policy or accidental parity drift.
