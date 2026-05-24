---
title: "feat: DeNCS /lfg post-merge verification on master"
type: feat
status: completed
date: 2026-05-23
origin: docs/NCS_DENCS_JAVA_ACCOUNTING.md
---

# feat: DeNCS /lfg post-merge verification on master

## Summary

Re-run the full `/lfg` completion checklist on `master` after merge of managed-only NCS policy (#10). Confirm product compile/decompile paths, Java accounting doc accuracy, and default-tier `KPatcher.Tests` pass via the repo test wrapper — with minimal doc-only updates if verification stamp needs refresh.

---

## Problem Frame

`/lfg` requires managed NSS/NCS tooling, no registry spoofing in product paths, Java file accounting, and passing NCS/NSS tests. Implementation landed at `1109c2d7`; this plan is an execution-time verification pass, not a greenfield port.

---

## Assumptions

*LFG pipeline (headless) mode.*

- `docs/NCS_DENCS_JAVA_ACCOUNTING.md` remains authoritative for Java→C# mapping.
- `vendor/DeNCS` may be empty locally; opt-in `NCSDecompCliRoundTripTest` is not a default gate.
- Fixes are limited to verification gaps (docs, broken references) — not re-architecting NCSDecomp.Core.

---

## Requirements

- R1. Product paths: `NCSCompiler`, `ModificationsNSS`, `ConfigReader`, `NCSManagedDecompiler` use managed tooling only; `CreateRegistrySpoofer` is no-op.
- R2. `docs/NCS_DENCS_JAVA_ACCOUNTING.md` reflects current master and lists ≥8 NCS/NSS test classes.
- R3. `./scripts/dotnet-test.sh KPatcher.sln -c Debug` passes (default runsettings).
- R4. No new registry-spoofer or `nwnnsscomp.exe` dependency introduced in product code.

---

## Scope Boundaries

- No line-by-line re-port of DeNCS Java.
- No requirement to populate `vendor/DeNCS` in this slice.
- No change to opt-in `DeNCSRoundTrip` / external-compiler test harness behavior.
- C# 7.3 cap unchanged.

### Deferred to Follow-Up Work

- CI `continue-on-error: true` on tests — separate hardening.
- K1 strict bytecode parity vs BioWare tool — parity ledger track.
- `docs/solutions/` compound entry for managed-only policy.

---

## Context & Research

### Relevant Code and Patterns

- `src/KPatcher.Core/Formats/NCS/Compiler/NCSCompiler.cs` — `NCSAuto.CompileNss`
- `src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs`
- `src/KPatcher.Core/Formats/NCS/Decompiler/NCSManagedDecompiler.cs`
- `src/NCSDecomp.Core/` — decompiler library
- `src/KCompiler.Core/`, `src/KCompiler.NET/` — managed compile
- `tests/KPatcher.Tests/Default.runsettings` — excludes `DeNCSRoundTrip`

### Institutional Learnings

- None in `docs/solutions/` for NCS/NSS (greenfield).

---

## Key Technical Decisions

- **Verification-only slice:** Treat merged work as complete; fail only on regression or policy violation found at runtime.
- **Test authority:** `scripts/dotnet-test.sh` is the only acceptable default test invocation.

---

## Open Questions

### Resolved During Planning

- **Re-port needed?** No — accounting doc and prior plan `004` mark port complete.

### Deferred to Implementation

- Whether vanilla managed round-trip tests skip due to empty `vendor/Vanilla_KOTOR_Script_Source` — note in verification summary only.

---

## Implementation Units

- U1. **Product-path policy audit**

**Goal:** Confirm no product code shells out to `nwnnsscomp.exe` or uses registry spoofing.

**Requirements:** R1, R4

**Dependencies:** None

**Files:**
- Inspect: `src/KPatcher.Core/Formats/NCS/Compiler/NCSCompiler.cs`
- Inspect: `src/KPatcher.Core/Mods/NSS/ModificationsNSS.cs`
- Inspect: `src/KPatcher.Core/Reader/ConfigReader.cs`
- Inspect: `src/NCSDecomp.Core/Utils/CompilerExecutionWrapper.cs`

**Approach:** Grep product paths; confirm `NoOpRegistrySpoofer` and `NCSAuto.CompileNss` / `NCSManagedDecompiler` only.

**Test scenarios:**
- Happy path: `CreateRegistrySpoofer()` returns no-op; `NCSCompiler` has no `Process.Start` for external compiler.

**Verification:** Audit notes recorded; no violations.

---

- U2. **Run default test suite**

**Goal:** Prove default-tier NCS/NSS tests pass on current tree.

**Requirements:** R3

**Dependencies:** U1

**Files:**
- Test: `tests/KPatcher.Tests/` (via solution)

**Approach:** Run `bash ./scripts/dotnet-test.sh KPatcher.sln -c Debug` once; if exit 124, diagnose bottleneck — do not skip tests.

**Test scenarios:**
- Happy path: Wrapper exits 0; filtered NCS/NSS tests pass.

**Verification:** Command exit code 0; test count logged.

---

- U3. **Refresh verification stamp**

**Goal:** Update authoritative accounting doc with this `/lfg` run date, commit hash, and test result.

**Requirements:** R2

**Dependencies:** U2

**Files:**
- Modify: `docs/NCS_DENCS_JAVA_ACCOUNTING.md`

**Approach:** Update `Last /lfg verification` section only if U2 passed.

**Test scenarios:**
- Test expectation: none — documentation only.

**Verification:** Doc mentions current date and master HEAD short SHA.

---

## System-Wide Impact

- **Unchanged invariants:** Patcher install, config reader, and managed decompiler integration remain as merged in #10.

---

## Risks & Dependencies

| Risk | Mitigation |
|------|------------|
| Empty vendor trees cause false confidence on vanilla round-trip | Document skip/no-op in verification summary |
| Long test run hits 600s cap | Use default runsettings; fix bottleneck if exit 124 |

---

## Sources & References

- **Origin document:** `docs/NCS_DENCS_JAVA_ACCOUNTING.md`
- Prior plan: `docs/plans/2026-05-23-004-feat-dencs-managed-port-verification-plan.md`
- Merge commit: `1109c2d7`
