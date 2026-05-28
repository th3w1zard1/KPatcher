---
title: "TSLPatcher Core Logic Parity Audit"
status: active
date: 2026-05-28
---

# TSLPatcher Core Logic Parity Audit

## Observation boundary

- [REPO] This pass compares KPatcher's behavior-owning install/config surfaces against repo-local parity documents and a locally checked-out TSLPatcher Delphi source tree used during the audit.
- [REPO] The comparison focused on the surfaces that decide behavior: install orchestration, namespace/config selection, required-file gating, backup semantics, and install safety checks.
- [REPO] The pass also spot-checked the historically tricky modifier features (`ExclusiveColumn`, `high()`, `RowLabel`, `2DAMEMORY`, `StrRef`, `!SourceFile`, `!SaveAs`, `!FieldPath`, `ReplaceFile`) and found corresponding support surfaces in KPatcher.
- [OPEN] This audit promotes only directly observed discrepancies. It does not claim byte-for-byte equivalence for every low-level algorithm inside every Delphi/Pascal format unit that was not read line by line in this slice.

## Confirmed findings

### 1. Pipeline truth is split across repo sources, and KPatcher does not fully match any of them

- [REPO] The older Delphi snapshot (`UTSLPatcher12.pas`) runs `TLK -> 2DA -> GFF -> HACK -> Compile -> InstallList`.
- [REPO] The newer Delphi snapshot (`UTSLPatcher.pas`) runs `TLK -> InstallList -> 2DA -> GFF -> HACK -> Compile -> SSF`.
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md) states the verified shipped binary runs `TLK -> GFF -> 2DA -> InstallList -> HACK -> NSS -> SSF`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) currently queues `TLK -> InstallList -> 2DA -> GFF -> NSS -> NCS -> SSF` and comments that this is the TSLPatcher order.
- [SYNTH] KPatcher does not currently match the binary-verified order, and the repo cannot honestly describe one single authoritative TSLPatcher pipeline without first deciding whether parity targets the verified binary or the reconstructed WIP Delphi source.
- [OPEN] A code-fix pass should not change patch ordering until the parity target is chosen explicitly.

### 2. Namespace fallback and path confinement diverge

- [REPO] The original namespace UI falls back to `changes.ini` / `info.rtf` when a namespace entry points at missing files, and it refuses `DataPath` values containing `..\` so a namespace cannot escape `tslpatchdata`.
- [REPO] [src/KPatcher.Core/Reader/NamespaceReader.cs](src/KPatcher.Core/Reader/NamespaceReader.cs) requires `IniName` and `InfoName` in every namespace section, and [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) resolves `DataFolderPath` directly through `Path.Combine` without an equivalent path-confinement guard.
- [SYNTH] KPatcher is stricter than TSLPatcher when namespace files are missing, but more permissive about namespace paths escaping `tslpatchdata`. Both are real behavior differences.
- [OPEN] If parity with original namespace behavior matters, KPatcher needs both a fallback decision and a path-confinement rule.

### 3. InstallList overwrite safety checks are missing on the KPatcher side

- [REPO] The newer Delphi source blocks `Replace#` installs from overwriting existing `.exe`, `.tlk`, `.key`, and `.bif` targets.
- [REPO] [src/KPatcher.Core/Mods/InstallFile.cs](src/KPatcher.Core/Mods/InstallFile.cs) is a passthrough copy operation, and the reviewed KPatcher install path only special-cases direct TLK patch writes to `dialog.tlk` in [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs).
- [REPO] No equivalent InstallList extension filter was found in the reviewed KPatcher C# path.
- [SYNTH] This is a confirmed parity and safety gap. KPatcher can presently replace targets that classic TSLPatcher explicitly refused to overwrite.
- [OPEN] Add a focused fix slice after the pipeline target is settled.

### 4. Backup semantics differ materially

- [REPO] The Delphi patch handler stores single-copy backups under the patcher application's `backup\` folder.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) creates timestamped `backup/<timestamp>` folders under the mod tree and clears a sibling `uninstall` directory before creating the new backup.
- [REPO] [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) exposes an uninstall workflow that consumes those backups; no Delphi uninstall workflow was found during the vendor-source grep pass.
- [SYNTH] KPatcher intentionally extends backup behavior to support uninstall, but that is not strict core-behavior parity and should not be summarized as "same as TSLPatcher."
- [OPEN] Keep this documented as an intentional extension unless the product decides to trade uninstall safety for stricter historical behavior.

### 5. The repo's parity documentation is overstated relative to the available evidence

- [REPO] [docs/PARITY_CONFIDENCE_LEDGER.md](docs/PARITY_CONFIDENCE_LEDGER.md) currently says parity is strong and frames the GFF/2DA ordering variant as the main known deviation.
- [REPO] [docs/TSLPATCHER_BUILD_VERIFICATION.md](docs/TSLPATCHER_BUILD_VERIFICATION.md) links `docs/TSLPATCHER_RE.md`, which is not present in the current tree.
- [REPO] The build-verification document's pipeline claim conflicts with the currently reviewed Delphi source snapshots.
- [SYNTH] The current parity story is stronger than the evidence supports. Summary docs need to be downgraded and pointed at this audit.

## Areas that look aligned enough not to raise as new gaps in this pass

- [REPO] Both codebases expose the same major patch surfaces: TLK, 2DA, GFF, HACK/NCS token patching, Compile/NSS, InstallList, and SSF.
- [REPO] Both sides expose `LookupGameFolder` / `LookupGameNumber` and required-file gating concepts in settings.
- [REPO] KPatcher contains support surfaces for the historically tricky modifier families that show up repeatedly in the Delphi changelog and code paths: `ExclusiveColumn`, `high()`, `RowLabel`, `2DAMEMORY`, `StrRef`, `!SourceFile`, `!SaveAs`, `!FieldPath`, and `ReplaceFile`.
- [SYNTH] This pass did not uncover a new handler-level blocker in those feature families, but that is weaker than a full algorithm-by-algorithm equivalence claim.

## Recommended follow-up slices

1. Choose the authoritative parity target for pipeline ordering: verified binary behavior or current reconstructed Delphi source.
2. Align KPatcher's InstallList overwrite safeguards with the historical `.exe` / `.tlk` / `.key` / `.bif` protections.
3. Decide whether KPatcher should adopt TSLPatcher-style namespace fallbacks and `DataPath` confinement.
4. Run a deeper low-level handler audit only after the top-level source-vs-runtime target is settled.