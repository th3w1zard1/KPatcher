---
title: "feat: DeNCS /lfg reverification on parity branch"
type: feat
status: completed
date: 2026-06-11
execution: code
---

# feat: DeNCS /lfg reverification on parity branch

## Summary

Re-run `/lfg` completion criteria for the DeNCS Java→C# port on branch `feat/parity-integration-tests-and-compile-serialize`. Prior sign-off on `master` at `15c03d8a` documented all criteria **Met**; this pass confirms no regression after TSLPatcher parity test expansion and refreshes `docs/NCS_DENCS_JAVA_ACCOUNTING.md` verification stats.

## Problem Frame

The `/lfg` command requires managed NSS/NCS tooling (KCompiler + NCSDecomp.Core), full Java accounting, ≥8 NCS/NSS test classes, and passing default-tier tests — without registry spoofing or default `nwnnsscomp.exe` dependency. This is a **verification and documentation** slice, not a re-port.

## Assumptions

- `docs/NCS_DENCS_JAVA_ACCOUNTING.md` remains authoritative for Java file mapping (271 sources).
- No new DeNCS Java files landed since last verification.
- Default tier uses `tests/KPatcher.Tests/Default.runsettings` (851 tests on current branch).

## Requirements

- R1. All 271 relevant DeNCS `.java` files remain accounted or superseded per accounting doc (no new gaps).
- R2. Product compile/decompile paths remain managed-only (`NCSCompiler`, `NCSManagedDecompiler`, `ModificationsNSS`); `CreateRegistrySpoofer()` stays no-op.
- R3. NCS/NSS filtered tests in `KPatcher.Tests` pass (managed roundtrip/compiler/format coverage).
- R4. `KCompiler.Tests` (6) and `NCSDecomp.Tests` (1) pass.
- R5. Default-tier `KPatcher.Tests` suite passes via repo wrapper.
- R6. Update verification section in `docs/NCS_DENCS_JAVA_ACCOUNTING.md` with branch, commit, and counts.

## Scope Boundaries

- No re-port of DeNCS Java sources.
- No TSLPatcher `ModInstaller` parity work (separate track).
- No enabling `nwnnsscomp.exe` in product paths.
- Opt-in `NCSDecompCliRoundTripTest` / Vendor tier not required for this sign-off.

## Implementation Units

### U1. Automated verification gate

Run:

```bash
bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug --settings tests/KPatcher.Tests/Default.runsettings --filter "FullyQualifiedName~NCS|FullyQualifiedName~NSS|FullyQualifiedName~KCompiler|FullyQualifiedName~NCSDecomp|FullyQualifiedName~Decomp"
bash ./scripts/dotnet-test.sh tests/KCompiler.Tests/KCompiler.Tests.csproj -c Debug
bash ./scripts/dotnet-test.sh tests/NCSDecomp.Tests/NCSDecomp.Tests.csproj -c Debug
bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug --settings tests/KPatcher.Tests/Default.runsettings
```

Confirm `NCSDecomp.Core` file count and grep product paths for external compiler/registry usage.

**Test file:** none (verification only).

### U2. Refresh accounting verification section

Update `docs/NCS_DENCS_JAVA_ACCOUNTING.md`:

- Last verification date, branch `feat/parity-integration-tests-and-compile-serialize`, HEAD commit.
- Default tier count (851), NCS/NSS filter count (226), satellite test counts.
- Re-affirm completion checklist rows remain **Met**.

**Test file:** none.

## Verification

All commands in U1 exit 0. Accounting doc reflects current branch stats.
