---
title: "fix: Linux-default test suite green for /lfg R3"
type: fix
status: completed
date: 2026-05-23
origin: docs/plans/2026-05-23-005-feat-dencs-lfg-post-merge-verification-plan.md
---

# fix: Linux-default test suite green for /lfg R3

## Summary

Third `/lfg` pass on `feat/lfg-dencs-post-merge-verification`: DeNCS managed-only policy is already merged (#10) and NCS/NSS tests pass. Requirement **R3** from plan `005` failed on Linux because ~27 default-tier tests assume Windows paths, case-insensitive renames, committed `test_files/`, or a present `Integration/` tree. This plan fixes or gates those tests so `./scripts/dotnet-test.sh KPatcher.sln -c Debug` exits 0 on Linux without weakening NCS/NSS coverage.

## Problem Frame

`/lfg` completion requires the wrapped full solution test run to pass. Prior verification logged 762 passed / 27 failed on Linux; failures are environmental, not NCS regressions. Agents and CI on Linux must get a truthful green default gate.

## Assumptions

- Managed-only NCS policy (R1, R4) remains unchanged.
- `Category!=DeNCSRoundTrip` and existing default filters stay.
- C# 7.3; stage commits per file.

## Requirements

- R1. Re-confirm product paths: no `nwnnsscomp.exe` shell-out; no-op registry spoofer.
- R2. `./scripts/dotnet-test.sh KPatcher.sln -c Debug` exits 0 on Linux (default runsettings).
- R3. NCS/NSS filtered tests still pass (regression check).
- R4. Update `docs/NCS_DENCS_JAVA_ACCOUNTING.md` verification stamp after R2.

## Scope Boundaries

- No DeNCS Java re-port.
- No enabling excluded `Integration/K1P*.cs` tests.
- Do not reintroduce `test_files/` committed fixtures.

### Deferred

- CI `continue-on-error: true` hardening.
- Opt-in exhaustive / vendor game categories.

## Implementation Units

### U1. Gate Windows-only path normalization tests

**Files:** `tests/KPatcher.Tests/Common/CaseAwarePathTests.cs`, `tests/KPatcher.Tests/Default.runsettings`

**Approach:** Add `[Trait("Category", "WindowsOnly")]` to `PathNormalization_EdgeCases` and `PathNormalization_PreservesCase`. Exclude `WindowsOnly` in default runsettings filter.

### U2. Fix format `TestWriteRaises` non-Windows expectations

**Files:** `tests/KPatcher.Tests/Formats/*FormatTests.cs` (GFF, ERF, RIM, TLK, SSF, TwoDA)

**Approach:** On non-Windows, accept `UnauthorizedAccessException` when writing to `.` (directory), matching actual OS behavior.

### U3. Gate case-insensitivity helper tests

**Files:** `tests/KPatcher.Tests/Common/SystemHelpersTests.cs`

**Approach:** Skip `FixCaseSensitivityRecursive_*` on case-sensitive filesystems (Linux default) via early return or `WindowsOnly` trait.

### U4. Cross-platform namespace path assertions

**Files:** `tests/KPatcher.Tests/Namespaces/PatcherNamespaceTests.cs`

**Approach:** Assert `ChangesFilePath` / `RtfFilePath` using `Path.Combine` expectations instead of hardcoded `C:\` strings.

### U5. Policy tests aligned with zero `test_files` policy

**Files:** `tests/KPatcher.Tests/Policies/TestFilesRootPolicyTests.cs`, `tests/KPatcher.Tests/Policies/IntegrationFolderNoMoqTests.cs`

**Approach:** Pass when `test_files` is absent (desired). Pass when `Integration/` is absent (integration sources compile-removed).

### U6. Verify and document

**Approach:** Run full wrapper + NCS filter; update accounting doc verification section.

## Test scenarios

- Default suite on Linux: exit 0.
- NCS filter: all matching tests pass.
- Windows-only tests still runnable with explicit filter `Category=WindowsOnly`.

## Risks

| Risk | Mitigation |
|------|------------|
| Over-skipping hides regressions | Only gate OS-specific behavior; NCS suite unchanged |
