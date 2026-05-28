---
title: "refactor: refresh TSLPatcher core logic parity audit"
type: refactor
status: active
date: 2026-05-27
origin: user request (/compound-engineering:lfg compare TSLPatcher to KPatcher core logic)
---

# refactor: refresh TSLPatcher core logic parity audit

## Summary

Refresh the repo's TSLPatcher parity story using direct core-logic comparison instead of carrying forward high-confidence claims from earlier audit docs. The output of this slice is a new source-backed audit document that compares behavior-owning TSLPatcher and KPatcher surfaces, plus a ledger update that removes or narrows any parity claims the current evidence does not support.

## Problem frame

The repository currently presents parity confidence as strong, but the accessible evidence already shows at least two material problems:

1. `docs/PARITY_CONFIDENCE_LEDGER.md` says KPatcher's core pipeline parity is strong even though `docs/TSLPATCHER_BUILD_VERIFICATION.md` documents a binary-confirmed TSLPatcher order of `TLK -> GFF -> 2DA -> InstallList -> HACK -> NSS -> SSF`, while `src/KPatcher.Core/Patcher/ModInstaller.cs` still queues `TLK -> InstallList -> 2DA -> GFF -> NSS -> NCS -> SSF`.
2. The ledger treats reverse-engineering reference material as complete, but at least one referenced source (`docs/TSLPATCHER_RE.md`) is missing from the current tree.

That means a fresh parity pass must prioritize evidence discipline over optimistic summaries. The goal is not to regress KPatcher toward Delphi-era messiness; it is to identify where core behavior does or does not match and record those findings accurately.

## Requirements

- R1. Compare behavior-owning TSLPatcher logic against KPatcher logic for the core install pipeline, not just project organization or naming.
- R2. Cover the main parity surfaces explicitly: config/settings parsing, namespace/config selection, pipeline order/orchestration, TLK/GFF/2DA/InstallList/HACKList/NSS/SSF handling, backup/uninstall behavior, and logging/progress semantics.
- R3. Distinguish confirmed discrepancies from open questions using evidence labels; do not overstate parity where direct source confirmation is missing.
- R4. Produce a durable audit artifact in the repo documenting gaps, discrepancies, and evidence.
- R5. Refresh `docs/PARITY_CONFIDENCE_LEDGER.md` so its parity summary and known deviations match the new audit findings.
- R6. Treat organization-only differences as non-findings unless they materially change behavior.
- R7. Use the locally available TSLPatcher source tree as a comparison input during execution; if a comparison surface cannot be confirmed directly, mark it open rather than inferring.

## Scope boundaries

- In scope: comparison and documentation of core logic behavior differences between TSLPatcher and KPatcher.
- In scope: correcting repo-local parity claims when the new audit disproves or weakens them.
- Out of scope: implementing parity fixes in KPatcher code during this slice.
- Out of scope: cosmetic UI-layout differences unless they affect workflow logic or state transitions.
- Out of scope: architecture cleanup or refactoring for its own sake.

## Key technical decisions

1. Create a new audit document instead of only editing the existing ledger.
   Rationale: the ledger is a summary surface; the new work needs a primary artifact that shows the comparison evidence and individual gaps in enough detail to support future fixes.

2. Refresh the ledger after the audit document is written.
   Rationale: the summary should reflect the new evidence, not lead it.

3. Compare behavior owners, not entire folders.
   Rationale: full accuracy comes from the code that decides behavior: `UTSLPatcher*.pas` / related Delphi units on one side and `ConfigReader`, `Core`, `ModInstaller`, and typed modification handlers on the other.

4. Treat missing or drifted reference documentation as an audit finding.
   Rationale: broken or absent references weaken parity confidence and should not be silently ignored.

## Implementation units

### U1. Build the core logic comparison audit

- Goal: produce a durable, evidence-labeled document mapping core TSLPatcher behavior to KPatcher behavior and listing confirmed gaps or discrepancies.
- Files:
  - `docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md`
- Patterns to follow:
  - Evidence labeling rules already active in repo instructions: `[REPO]`, `[SYNTH]`, `[OPEN]`
  - Existing parity docs in `docs/TSLPATCHER_BUILD_VERIFICATION.md` and `docs/PARITY_CONFIDENCE_LEDGER.md`
- Approach:
  - Read the TSLPatcher behavior-owning Delphi units available in the local reference tree during execution.
  - Read the KPatcher behavior-owning C# files for the matching surfaces.
  - For each core surface, record: TSLPatcher behavior, KPatcher behavior, match/mismatch status, evidence, and why the difference matters or does not.
  - Explicitly separate confirmed gaps from unresolved questions.
- Test scenarios:
  - The audit names each required comparison surface from R2.
  - Each confirmed discrepancy cites both the TSLPatcher and KPatcher logic source used to support it.
  - Organization-only differences are excluded unless behavior changes.
  - Missing reference docs or drifted repo claims are recorded as findings, not omitted.
- Verification:
  - Read the completed audit doc end to end and verify every high-severity claim is evidence-backed.

### U2. Refresh the parity confidence ledger

- Goal: make the ledger summary consistent with the new audit and downgrade or sharpen claims where evidence changed.
- Files:
  - `docs/PARITY_CONFIDENCE_LEDGER.md`
- Patterns to follow:
  - Existing ledger structure and summary style
  - New audit findings from U1
- Approach:
  - Replace overconfident parity summary text with a source-backed status.
  - Update the execution-pipeline section and known-deviations section to reflect confirmed findings from U1.
  - Add a reference to the new audit doc so future readers land on the primary evidence.
- Test scenarios:
  - Ledger summary no longer claims stronger parity than the audit supports.
  - Pipeline order and known deviations match the new audit.
  - The ledger points readers to the new audit artifact for detail.
- Verification:
  - Focused diff review of the updated ledger sections.

## Risks and mitigations

- Risk: the locally available TSLPatcher reference source differs from the binary-confirmed behavior docs.
  Mitigation: record source-vs-doc drift explicitly and prefer direct evidence over earlier summaries.

- Risk: some KPatcher surfaces have Python-port behavior that differs from original TSLPatcher for documented reasons.
  Mitigation: classify them as intentional or unexplained only when the evidence supports that distinction.

- Risk: the audit becomes a generic architecture summary instead of a gap report.
  Mitigation: each section must end in a parity judgment or an explicit open question.

## Validation plan

1. Verify the new audit doc covers every required core surface.
2. Verify `docs/PARITY_CONFIDENCE_LEDGER.md` no longer overstates parity relative to the new audit.
3. Run `git diff --check` on the changed documentation files.

## Status deltas

- Landed: plan created.
- Partial/uncertain: the full discrepancy set still depends on direct side-by-side reads of the Delphi reference units during execution.
- Next-step change: build the audit document first, then refresh the ledger summary from that evidence.