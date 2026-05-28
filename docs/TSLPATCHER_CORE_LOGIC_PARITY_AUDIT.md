---
title: "TSLPatcher Core Logic Parity Audit"
status: active
date: 2026-05-28
---

# TSLPatcher Core Logic Parity Audit

## Observation boundary

- [REPO] This branch-local audit compares KPatcher's behavior-owning install, config, namespace, GFF, and 2DA surfaces against the vendored Delphi source tree under `vendor/TSLPatcher`.
- [REPO] It records only branch-local parity claims that are backed by code in this worktree plus focused tests.
- [OPEN] It does not claim end-to-end exact parity for every TSLPatcher behavior. Remaining unresolved slices stay listed as open gaps until code and tests land here.

## Landed parity slices on this branch

### 1. Top-level GFF `!FieldPath` handling now matches the vendored owner path

- [REPO] The vendored Delphi path stores `!FieldPath` only through recursive `AddGffField(...)` handling when new GFF fields are added.
- [REPO] [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs) no longer special-cases top-level `GFFList` keys named `2DAMEMORY#` into `Memory2DAModifierGFF` behavior.
- [REPO] [tests/KPatcher.Tests/Reader/ConfigReaderGFFTests.cs](tests/KPatcher.Tests/Reader/ConfigReaderGFFTests.cs) now proves that a top-level `2DAMEMORY#=!FieldPath` entry stays a regular `ModifyFieldGFF` with literal path/value semantics.
- [SYNTH] KPatcher previously overreached beyond the vendored owner path here; that drift is now removed.

### 2. Namespace fallback and `DataPath` confinement are aligned for the reviewed owner paths

- [REPO] The vendored namespace UI falls back to `changes.ini` and `info.rtf` when namespace metadata is missing or points at absent files, and it rejects `DataPath` values that back out of `tslpatchdata`.
- [REPO] [src/KPatcher.Core/Reader/NamespaceReader.cs](src/KPatcher.Core/Reader/NamespaceReader.cs) now defaults missing `IniName` / `InfoName` values and strips `DataPath` values containing `..` segments.
- [REPO] [src/KPatcher.UI/Core.cs](src/KPatcher.UI/Core.cs) now falls back from missing namespace-specific `changes` and `info.rtf` targets to the default `tslpatchdata` files.
- [REPO] [tests/KPatcher.Tests/Reader/NamespaceReaderTests.cs](tests/KPatcher.Tests/Reader/NamespaceReaderTests.cs) and [tests/KPatcher.Tests/UI/CoreNamespaceFallbackTests.cs](tests/KPatcher.Tests/UI/CoreNamespaceFallbackTests.cs) exercise both the parser fallback and the runtime namespace-file fallback.
- [SYNTH] The earlier mismatch is closed for the reviewed namespace owner path.

### 3. InstallList overwrite safeguards now match the reviewed vendored protections

- [REPO] The vendored `DoInstallFiles()` path blocks folder-target replacement of existing `.exe`, `.tlk`, `.key`, and `.bif` files.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) now blocks those same extensions for folder-based `InstallFile` replacements while still allowing archive-target behavior.
- [REPO] [tests/KPatcher.Tests/Patcher/ModInstallerTests.cs](tests/KPatcher.Tests/Patcher/ModInstallerTests.cs) covers the protected-extension skip cases and the archive-target allow case.
- [SYNTH] KPatcher no longer overwrites InstallList targets that the reviewed vendored path explicitly refuses to replace.

### 4. KPatcher-only K1 2DA hardcaps have been removed

- [REPO] [src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs](src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs) previously rejected K1 patch results for `placeables.2da`, `upcrystals.2da`, and `upgrade.2da` once row counts exceeded hardcoded limits.
- [REPO] No equivalent vendor-side guard was found in the reviewed 2DA owner paths under `vendor/TSLPatcher`.
- [REPO] [tests/KPatcher.Tests/Mods/TwoDAModsUnitTests.cs](tests/KPatcher.Tests/Mods/TwoDAModsUnitTests.cs) now proves those filenames still preserve patched output on K1 after crossing the former limits.
- [SYNTH] This KPatcher-specific policy branch has been removed from core 2DA patch logic.

### 5. Installer queue order now matches the binary-verified TSLPatcher runtime order

- [REPO] Repo-local verification in `docs/TSLPATCHER_BUILD_VERIFICATION.md` identifies the shipped runtime queue as `TLK -> GFF -> 2DA -> InstallList -> HACK -> Compile -> SSF`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) now applies patches in that same order.
- [SYNTH] Queue-order parity for the shipped runtime path is now closed on this branch.

### 6. HACKList, CompileList, and SSF owner-path behavior has been tightened toward the reviewed vendor path

- [REPO] [src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs](src/KPatcher.Core/Mods/NCS/ModificationsNCS.cs) now treats plain HACK entries as vendor-style 32-bit writes while preserving explicit KPatcher typed extensions.
- [REPO] [src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs](src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs) and [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) now align CompileList prep root, include-file detection, temp layout, and compile-failure skip behavior more closely with the reviewed vendor flow.
- [REPO] [src/KPatcher.Core/Reader/ConfigReader.cs](src/KPatcher.Core/Reader/ConfigReader.cs), [src/KPatcher.Core/Mods/SSF/ModificationsSSF.cs](src/KPatcher.Core/Mods/SSF/ModificationsSSF.cs), and the SSF format owners now recover from missing modifier sections and invalid stringrefs while carrying the full 40-entry soundset layout the vendored SSF handler supports.
- [REPO] SSF characterization coverage now exists in [tests/KPatcher.Tests/Formats/SSFFormatTests.cs](tests/KPatcher.Tests/Formats/SSFFormatTests.cs), [tests/KPatcher.Tests/Mods/SsfModificationTests.cs](tests/KPatcher.Tests/Mods/SsfModificationTests.cs), and [tests/KPatcher.Tests/Reader/ConfigReaderSSFTests.cs](tests/KPatcher.Tests/Reader/ConfigReaderSSFTests.cs).
- [SYNTH] These slices are no longer open at the owner-path level, though broader generic HACKList and external-compiler parity questions remain.

### 7. Override conflict defaults now match the reviewed vendored behavior

- [REPO] The reviewed `HandleERFOverrideType(...)` vendored path defaults absent `!OverrideType` to `ignore` and renames conflicting files to `old_<name>` without iterative suffix generation.
- [REPO] [src/KPatcher.Core/Mods/PatcherModifications.cs](src/KPatcher.Core/Mods/PatcherModifications.cs) now defaults `OverrideTypeValue` to `ignore`.
- [REPO] [src/KPatcher.Core/Patcher/ModInstaller.cs](src/KPatcher.Core/Patcher/ModInstaller.cs) now uses the vendored single-target rename behavior.
- [SYNTH] KPatcher no longer warns by default or invents numbered override rename targets where the reviewed vendor path would not.

## Remaining confirmed gaps

### 1. KPatcher still narrows generic HACKList behavior to NCS-only patching

- [REPO] The reviewed vendored HACKList path operates as a generic binary offset patcher.
- [REPO] KPatcher still routes `[HACKList]` entries into `ModificationsNCS` behavior rather than a generic binary patch surface.
- [OPEN] Exact parity is still missing here.

### 2. CompileList orchestration and settings still differ where TSLPatcher shells an external compiler

- [REPO] The reviewed vendored path shells an external compiler and exposes settings such as `ScriptCompilerFlags`.
- [REPO] KPatcher still uses the managed in-process compiler path.
- [OPEN] This remains an intentional architecture difference unless later parity work reproduces the missing configuration semantics.

### 3. Backup and uninstall semantics still extend beyond TSLPatcher

- [REPO] KPatcher retains timestamped backup and uninstall behavior that is broader than the reviewed TSLPatcher backup path.
- [OPEN] This is still a product extension rather than strict parity.

## Recommended next slices

1. Choose and document the authoritative parity target for pipeline ordering, then align KPatcher's install queue to that exact target.
2. Decide whether strict parity requires replacing the NCS-only HACKList path with a generic binary patch surface.
3. Decide whether CompileList parity means reproducing missing external-compiler settings or keeping the managed compiler as a documented divergence.
4. Revisit backup/uninstall behavior only if strict parity is allowed to displace the current recoverability guarantees.