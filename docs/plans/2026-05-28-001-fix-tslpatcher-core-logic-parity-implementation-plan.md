---
title: "fix: implement TSLPatcher core logic parity slices"
type: fix
status: active
date: 2026-05-28
origin: user request (/compound-engineering:lfg compare and align TSLPatcher core logic)
audit_ref: docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md
---

# fix: implement TSLPatcher core logic parity slices

## Summary

Translate the confirmed parity gaps from the audit into executable KPatcher changes until the managed implementation matches vendored `vendor/TSLPatcher` core logic as closely as the reviewed owner paths allow. The branch keeps KPatcher's code organization, but removes behavior drift where vendored TSLPatcher logic is the clearer authority, starting with the already-landed `!FieldPath`, namespace, and InstallList slices and continuing with the next smallest confirmed mismatches.

## Problem frame

The audit slice established that KPatcher's parity story is weaker than the repo previously claimed, but it deliberately stopped short of code changes. The current request goes further: stop treating the discrepancies as documentation-only and make KPatcher match TSLPatcher core logic without recreating Delphi-era structure.

There are two important constraints on the implementation work:

1. The current audit called `!FieldPath` a resolved non-gap, but the user explicitly disputes that conclusion. That means `!FieldPath` must be re-characterized directly against vendored TSLPatcher before any other parity claim is trusted.
2. Not every previously-audited gap has the same implementation readiness. `!FieldPath`, namespace handling, and InstallList safety have clear behavior owners in the C# tree. Pipeline ordering, generic HACKList semantics, and CompileList settings still involve source-vs-runtime authority questions or materially larger execution surfaces.

## Requirements

- R1. Re-open `!FieldPath` parity by comparing vendored TSLPatcher behavior to KPatcher parsing and runtime behavior, then fix KPatcher if the semantics differ.
- R2. Add characterization-style tests for every behavior change so the parity target is executable, not only described.
- R3. Align high-confidence core-logic gaps with clear C# owners in this branch: `!FieldPath`, namespace fallback/path confinement, InstallList overwrite guards, and KPatcher-specific K1 2DA hardcaps that are not present in the reviewed vendor source.
- R4. Keep KPatcher's internal organization and managed implementation style where behavior can still match TSLPatcher.
- R5. Preserve repo constraints: C# 7.3 only, no new committed external fixture files, and real temp directories instead of mocking for installer-style tests.
- R6. Update parity documentation only where the implementation changes invalidate or sharpen current audit wording.
- R7. When repo-local TSLPatcher artifacts disagree, use the vendored `vendor/TSLPatcher` source tree as the primary code-level comparison target for this branch unless a narrower runtime-proven behavior is being fixed explicitly.

## Scope boundaries

- In scope: code, tests, and tightly-coupled documentation updates for confirmed parity gaps with a clear owning abstraction.
- In scope: reclassifying the `!FieldPath` audit conclusion if direct source comparison shows KPatcher is not actually equivalent.
- Out of scope in this branch: generic HACKList re-architecture, external-compiler parity for CompileList, or a patch-order rewrite while the authority question remains open.
- Out of scope in this branch: removing timestamped backup/uninstall support unless a directly observed parity defect proves that the extension changes patch results rather than post-install recoverability.

## Key technical decisions

1. Create a new implementation branch instead of extending the open docs-only audit PR.
   Rationale: the audit PR is already serving as the evidence artifact. Mixing code fixes into that branch would blur review scope and make the parity docs harder to trust as a historical record.

2. Treat `vendor/TSLPatcher` as the first authority for code-semantic parity in this branch.
   Rationale: the user explicitly wants KPatcher to match TSLPatcher. The vendored source is the directly accessible implementation surface for behavior-level comparison, while older binary-verification notes remain important context but not a sufficient basis for speculative code changes.

3. Use characterization-first edits for each parity slice.
   Rationale: several areas already had incorrect or overconfident parity summaries. Each slice should start by expressing the claimed TSLPatcher behavior in a focused test before changing production code.

4. Land exact-parity work in ascending owner clarity.
  Rationale: the user requirement is exact vendor logic, but some discrepancies still have radically different implementation cost. This branch should keep consuming the smallest directly evidenced drifts first (`!FieldPath`, namespace resolution, InstallList overwrite rules, K1 hardcaps) before reopening the broader HACKList/CompileList/pipeline questions.

## Implementation units

### U1. Re-characterize and align `!FieldPath` behavior

- Goal: determine whether KPatcher's `2DAMEMORY#=!FieldPath` parsing and runtime path resolution actually matches vendored TSLPatcher, then close the gap if it does not.
- Files:
  - `src/KPatcher.Core/Reader/ConfigReader.cs`
  - `src/KPatcher.Core/Mods/GFF/ModifyGFF.cs`
  - `tests/KPatcher.Tests/Reader/ConfigReaderGFFTests.cs`
  - `tests/KPatcher.Tests/Reader/ConfigReaderGFFPathTests.cs`
  - `tests/KPatcher.Tests/Mods/GffModsTests.cs`
- Reference anchors:
  - `vendor/TSLPatcher/UTSLPatcher.pas`
  - `vendor/TSLPatcher/UTSLPatcher12.pas`
- Approach:
  - Read the Delphi handling path for field-path storage and dereference.
  - Express the observed semantics in focused C# tests first, especially around what is stored in 2DA memory, when the path is resolved, and how nested/list paths behave.
  - Change KPatcher parser/runtime behavior only where the tests show a real mismatch.
- Test scenarios:
  - Parsing `2DAMEMORY#=!FieldPath` creates the same semantic modifier shape expected by vendored TSLPatcher.
  - Stored path tokens survive through add/modify/list operations and dereference the same target field path TSLPatcher would use.
  - Invalid or unsupported path shapes fail in the same place and mode as the target behavior, or are explicitly documented if KPatcher keeps a stricter guard.
- Verification:
  - Focused `ConfigReaderGFF*` and `GffMods*` test execution.

### U2. Align namespace fallback and `DataPath` confinement

- Goal: make KPatcher's namespace selection behave like vendored TSLPatcher when namespace metadata is omitted or attempts to escape `tslpatchdata`.
- Files:
  - `src/KPatcher.Core/Reader/NamespaceReader.cs`
  - `src/KPatcher.Core/Namespaces/PatcherNamespace.cs`
  - `src/KPatcher.UI/Core.cs`
  - `tests/KPatcher.Tests/Reader/NamespaceReaderTests.cs`
  - `tests/KPatcher.Tests/Reader/NamespaceReaderIniSnippetTableTests.cs`
  - `tests/KPatcher.Tests/Common/LocalizedConfigResolverTests.cs`
- Reference anchors:
  - `vendor/TSLPatcher/UNamespaceForm.pas`
  - `vendor/TSLPatcher/UMainForm.pas`
- Approach:
  - Add or revise tests for missing `IniName` / `InfoName` fallback behavior.
  - Characterize the `DataPath` escape rules from the Delphi UI units and enforce equivalent confinement in KPatcher's resolution path.
  - Keep localized-file resolution behavior only where it does not violate the confinement rule.
- Test scenarios:
  - Namespaces without explicit `IniName` / `InfoName` fall back to `changes.ini` / `info.rtf`.
  - Relative namespace paths that escape `tslpatchdata` are rejected.
  - Valid in-tree namespace data folders continue to resolve localized/base files correctly.
- Verification:
  - Focused `NamespaceReader*` and `LocalizedConfigResolverTests` execution.

### U3. Align InstallList overwrite safeguards

- Goal: prevent KPatcher InstallList operations from overwriting targets that vendored TSLPatcher historically refused to replace.
- Files:
  - `src/KPatcher.Core/Mods/InstallFile.cs`
  - `src/KPatcher.Core/Patcher/ModInstaller.cs`
  - `tests/KPatcher.Tests/Patcher/ModInstallerTests.cs`
  - `tests/KPatcher.Tests/Patcher/InstallFlightRecorderIntegrationTests.cs`
  - `tests/KPatcher.Tests/Reader/ConfigReaderIniSnippetTableTests.cs`
- Reference anchors:
  - `vendor/TSLPatcher/UTSLPatcher.pas`
- Approach:
  - Add coverage for `Replace#` writes targeting protected extensions.
  - Introduce the narrow extension guard at the InstallList behavior owner rather than spreading checks across unrelated patch types.
  - Preserve direct TLK-patching behavior while preventing InstallList copies from replacing `.exe`, `.tlk`, `.key`, and `.bif` files.
- Test scenarios:
  - Protected extensions are blocked when an InstallList entry would overwrite an existing target.
  - Non-protected extensions still install normally.
  - Existing TLK patch logic continues to modify `dialog.tlk` through the TLK path rather than the InstallList copy path.
- Verification:
  - Focused `ModInstallerTests` execution, with one integration-style temp-directory case if unit-level coverage alone is ambiguous.

### U4. Refresh parity documentation for implemented slices only

- Goal: keep the parity audit and ledger accurate after code changes without reopening the whole research pass.
- Files:
  - `docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md`
  - `docs/PARITY_CONFIDENCE_LEDGER.md`
- Approach:
  - Update only the findings touched by U1-U3.
  - Remove any stale claim that a fixed slice is still open, and downgrade any slice that remains unresolved after implementation.
- Test scenarios:
  - Documentation matches the landed code for `!FieldPath`, namespace behavior, and InstallList safeguards.
  - No broader parity claim is strengthened without corresponding code or evidence.
- Verification:
  - Focused diff review plus `git diff --check` on touched docs.

### U5. Remove KPatcher-specific K1 2DA hardcaps

- Goal: align 2DA patch application with vendored TSLPatcher by removing KPatcher-only row-limit rejection for `placeables.2da`, `upcrystals.2da`, and `upgrade.2da` on K1.
- Files:
  - `src/KPatcher.Core/Mods/TwoDA/Modifications2DA.cs`
  - `tests/KPatcher.Tests/Mods/TwoDAModsUnitTests.cs`
  - `tests/KPatcher.Tests/Mods/TwoDaModsTests.cs`
- Reference anchors:
  - `vendor/TSLPatcher/UTSLPatcher.pas`
  - `vendor/TSLPatcher/U2DAEdit.pas`
- Approach:
  - Re-characterize the current KPatcher hardcap path in focused tests.
  - Verify that the reviewed vendor 2DA patch path contains no equivalent K1-specific row-limit rejection.
  - Remove the row-limit guard from the KPatcher 2DA patch write path if the tests confirm it is a vendor-divergent add-on.
- Test scenarios:
  - K1 2DA patch application does not reject `placeables.2da`, `upcrystals.2da`, or `upgrade.2da` solely for exceeding KPatcher's current hardcoded row limits.
  - Existing 2DA modifier ordering and token-storage behavior remain intact after removing the hardcap branch.
- Verification:
  - Focused `TwoDAMods*` execution covering the touched K1 write path.

## Risks and mitigations

- Risk: vendored TSLPatcher source still contains behavior drift from the historically shipped binary.
  Mitigation: keep the implementation branch limited to slices where the vendored source gives a direct, behavior-owning answer; leave pipeline-order changes out until authority is clearer.

- Risk: `!FieldPath` turns out to differ in a more structural way than the audit assumed.
  Mitigation: characterize first, then change the smallest owning abstraction rather than patching call sites.

- Risk: namespace confinement changes could break existing KPatcher tests that intentionally rely on broader path acceptance.
  Mitigation: update tests only where the new behavior is directly justified by vendored TSLPatcher logic, and document any intentional retained divergence.

- Risk: InstallList safety guards could accidentally block unrelated patch paths.
  Mitigation: place the guard at the InstallList behavior boundary and cover allowed versus blocked targets explicitly.

- Risk: removing the K1 hardcap guard could expose a product-compatibility concern that KPatcher previously treated as policy.
  Mitigation: anchor the change on vendor-source absence plus focused K1 test coverage, and document any remaining compatibility concern as a post-parity product decision rather than keeping it hidden in core patch logic.

## Validation plan

1. Run the narrowest affected KPatcher test files for each implementation unit after its first substantive edit.
2. Run a focused build or error scan for touched C# files if a unit has no narrow executable test.
3. Run `git diff --check` on touched files before review/push.
4. Update the audit/ledger only after the code-backed behavior is validated.

## Status deltas

- Landed: implementation branch/worktree created; vendored TSLPatcher source initialized; `!FieldPath`, namespace fallback/path confinement, InstallList overwrite safeguards, and removal of KPatcher-only K1 hardcaps are all landed on this branch.
- Landed: additional owner-path parity fixes now cover the binary-verified install queue order, vendor-default HACK write semantics within the existing NCS-backed surface, CompileList prep/include/failure behavior, SSF recovery plus 40-slot handling, vendored `!OverrideType` default/rename behavior, and removal of unsupported top-level `!DefaultDestination` handling outside CompileList.
- Partial/uncertain: generic HACKList behavior, external-compiler/settings parity for CompileList, and backup/uninstall semantics still need branch-local code decisions or follow-up fixes.
- Next-step change: continue the remaining HACK/Compile/backup parity slices and keep the audit/ledger aligned with the landed owner-path fixes.
