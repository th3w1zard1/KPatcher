---
title: "feat: TSLPatcher core logic implementation and PR merge"
type: feat
status: superseded
date: 2026-06-10
origin: user /lfg request — iterative TSLPatcher↔KPatcher parity, merge open PRs
---

# feat: TSLPatcher core logic implementation and PR merge

## Summary

Land remaining TSLPatcher core-logic parity from `vendor/TSLPatcher/*.pas` into `src/KPatcher.Core`, fix the open parity-fixes PR CI failure, merge PR #15 (audit) and PR #16 (fixes), and continue alternating Delphi/C# reads for any remaining behavioral gaps.

## Problem frame

Open PR #16 (`feat/tslpatcher-core-logic-parity-fixes`) fails CI on `Install_HackListRenamedSource_UsesVendorCopyToOverrideMessage` because `ModInstaller` does not auto-resolve `TslPatchDataPath` when `changes.ini` lives under `tslpatchdata/`, so HACK sources beside the INI are invisible to `ResolveModContentPath`. PR #15 documents parity state; both should merge after green tests.

## Requirements

- R1. Fix HACKList renamed-source install path and vendored copy note parity (UTSLPatcher.pas `fileHack` dlgopen path).
- R2. Run full `KPatcher.Tests` via `scripts/dotnet-test.sh` and ensure CI-green on parity-fixes branch.
- R3. Merge PR #15 then PR #16 (or rebase fixes onto master after audit merge).
- R4. Continue pas↔C# comparison for InstallList guards, namespace/config, and remaining ledger gaps.
- R5. Managed NCS/NSS only (KCompiler + NCSDecomp.Core); no registry spoofer or `nwnnsscomp.exe` in product paths.

## Scope boundaries

- In scope: core patcher logic in `KPatcher.Core`, parity tests, docs ledger alignment.
- Out of scope: Avalonia UI layout, full generic HACKList beyond NCS surface, external compiler settings parity.

## Implementation units

### U1. Auto-resolve tslpatchdata root for ModInstaller

- Goal: resolve mod assets beside `changes.ini` without requiring UI to set `TslPatchDataPath`.
- Files: `src/KPatcher.Core/Patcher/ModInstaller.cs`
- Approach: after `changesIniPath` is finalized, set `TslPatchDataPath` to its directory when unset.
- Test scenarios: `Install_HackListRenamedSource_UsesVendorCopyToOverrideMessage` passes; missing-source HACK test still errors.
- Verification: filtered + full wrapped test run.

### U2. CI fix verification and PR merge

- Goal: green CI on parity-fixes; merge audit PR #15 and fixes PR #16.
- Files: branch `feat/tslpatcher-core-logic-parity-fixes`
- Verification: `gh pr checks` all pass; merge to master.

### U3. Iterative parity slice (InstallList / namespace)

- Goal: align next confirmed gap from pas audit vs ledger.
- Files: as identified by alternating reads (`UTSLPatcher.pas` ↔ `ModInstaller.cs` / `ConfigReader.cs`).
- Verification: targeted tests + ledger note if behavior intentionally extended.

## Risks

- Auto `TslPatchDataPath` may change layouts where mod root intentionally differs from INI directory — mitigate by only defaulting when property is null/empty.
