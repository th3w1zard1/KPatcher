---
title: "feat: DeNCS /lfg reverification on master (post install-path parity)"
type: feat
status: completed
date: 2026-06-13
execution: code
---

# feat: DeNCS /lfg reverification on master

## Summary

Re-run `/lfg` DeNCS completion criteria on `master` after merge of PR #20 (install-path parity harness). Prior sign-off documented all criteria **Met** at `e1e5eb34` on a parity branch; this pass confirms no regression on current `master` and refreshes `docs/NCS_DENCS_JAVA_ACCOUNTING.md` verification stats.

## Problem Frame

The `/lfg` cursor command requires: full Java accounting, managed NSS/NCS product paths (KCompiler + NCSDecomp.Core), ≥8 NCS/NSS test classes, passing default-tier tests, no registry spoofing, no default `nwnnsscomp.exe` dependency. This slice is **verification and documentation** — not a re-port.

## Assumptions

- `docs/NCS_DENCS_JAVA_ACCOUNTING.md` remains authoritative for 271 Java → C# mapping.
- `vendor/DeNCS` may be absent locally; accounting doc still valid from repo history.
- Default tier uses `tests/KPatcher.Tests/Default.runsettings`.

## Requirements

- R1. All 271 DeNCS `.java` files remain accounted or superseded (no new gaps in accounting doc).
- R2. Product compile/decompile paths remain managed-only; `CreateRegistrySpoofer()` stays no-op.
- R3. NCS/NSS filtered tests in `KPatcher.Tests` pass.
- R4. `KCompiler.Tests` and `NCSDecomp.Tests` pass.
- R5. Full default-tier `KPatcher.Tests` passes via repo wrapper.
- R6. Update verification section in `docs/NCS_DENCS_JAVA_ACCOUNTING.md` with `master` HEAD, commit, and test counts.

## Scope Boundaries

- No re-port of DeNCS Java sources.
- No KEditChanges or TSLPatcher install-path work in this PR.
- No enabling `nwnnsscomp.exe` in product paths.
- Opt-in `DeNCSRoundTrip` / `Vendor` tiers not required for sign-off.

## Implementation Units

### U1. Automated verification gate

Run:

```bash
bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug --settings tests/KPatcher.Tests/Default.runsettings --filter "FullyQualifiedName~NCS|FullyQualifiedName~NSS|FullyQualifiedName~KCompiler|FullyQualifiedName~NCSDecomp|FullyQualifiedName~Decomp"
bash ./scripts/dotnet-test.sh tests/KCompiler.Tests/KCompiler.Tests.csproj -c Debug
bash ./scripts/dotnet-test.sh tests/NCSDecomp.Tests/NCSDecomp.Tests.csproj -c Debug
bash ./scripts/dotnet-test.sh tests/KPatcher.Tests/KPatcher.Tests.csproj -c Debug --settings tests/KPatcher.Tests/Default.runsettings
```

Grep product paths: `ModificationsNSS`, `NCSManagedDecompiler`, `CreateRegistrySpoofer` → no-op only.

**Test file:** none (verification only).

### U2. Refresh accounting verification section

Update `docs/NCS_DENCS_JAVA_ACCOUNTING.md` with:

- Last verification date, branch `master`, HEAD commit.
- Default tier count, NCS/NSS filter count, satellite test counts.
- Re-affirm completion checklist rows remain **Met**.

**Test file:** none.

## Verification

All U1 commands exit 0. Accounting doc reflects current `master` stats.

## Test Scenarios

| ID | Scenario | Expected |
|----|----------|----------|
| T1 | NCS/NSS filter suite | All tests pass |
| T2 | KCompiler.Tests | 6 pass |
| T3 | NCSDecomp.Tests | 1 pass |
| T4 | Default KPatcher.Tests | Full suite pass (no timeout) |
| T5 | Registry spoofer grep | `CreateRegistrySpoofer` → `NoOpRegistrySpoofer` only in product wrapper |
