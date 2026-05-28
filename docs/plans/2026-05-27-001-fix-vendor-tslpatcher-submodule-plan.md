---
title: "fix: vendor TSLPatcher submodule metadata"
type: fix
status: active
date: 2026-05-27
origin: user request (/compound-engineering:lfg vendor TSLPatcher submodule)
---

# fix: vendor TSLPatcher submodule metadata

## Summary

The repository already records multiple `vendor/` gitlinks in the git tree, but `.gitmodules` is blank, so `git submodule status` fails immediately. At the same time, `README.md` describes `vendor/TSLPatcher` as a checked-in vendor tree even though the path is absent from the working tree. This slice restores usable submodule metadata, adds `vendor/TSLPatcher` as a real git submodule pointing at `https://github.com/OpenKotOR/TSLPatcher.git`, and aligns the repo documentation with the actual vendor layout.

## Requirements

| ID | Requirement |
|----|-------------|
| R1 | Restore `.gitmodules` entries for the existing vendor gitlinks already tracked in the repo tree. |
| R2 | Add `vendor/TSLPatcher` as a git submodule pointing to `https://github.com/OpenKotOR/TSLPatcher.git`. |
| R3 | Keep the change limited to git metadata and documentation; do not disturb unrelated dirty-worktree changes. |
| R4 | Update repo documentation so the vendor-submodule instructions describe the actual submodule topology after the change. |
| R5 | Verify that submodule commands now resolve cleanly for the tracked vendor paths. |

## Scope

- In scope: `.gitmodules`, vendor gitlink registration, minimal vendor-doc updates tied to submodule setup.
- Out of scope: changing vendor source contents, rewriting vendor verification scripts, or touching unrelated CI/test work already present in the checkout.

## Key technical decisions

1. Rebuild `.gitmodules` from the repo's tracked gitlinks instead of only appending `vendor/TSLPatcher`.
   Rationale: the current root failure is broader than the missing TSLPatcher path; `git submodule status` already breaks on `vendor/DeNCS` because no mappings exist.

2. Treat `vendor/TSLPatcher` as a submodule, not a checked-in directory.
   Rationale: that matches the user's request and keeps vendor references consistent with the repo's existing gitlink-based vendor strategy.

3. Limit documentation edits to the vendor-submodule section in `README.md`.
   Rationale: this change is infrastructure metadata, not product behavior, so broader docs churn is unnecessary.

## Implementation units

### U1: Reconstruct vendor submodule metadata

- Goal: make `.gitmodules` describe the vendor gitlinks already stored in the repo tree and add the new TSLPatcher mapping.
- Files:
  - `.gitmodules`
- Patterns to follow:
  - Existing `vendor/` gitlink paths from the repo tree
  - Repo convention of keeping vendor references under `vendor/`
- Approach:
  - Enumerate the gitlinks already present under `vendor/`.
  - Add matching `.gitmodules` entries for those paths.
  - Add the new `vendor/TSLPatcher` entry pointing at `https://github.com/OpenKotOR/TSLPatcher.git`.
- Test scenarios:
  - `git submodule status` no longer errors on missing mappings.
  - `.gitmodules` contains one section per tracked vendor gitlink plus `vendor/TSLPatcher`.
- Verification:
  - `git submodule status`

### U2: Register the new TSLPatcher gitlink

- Goal: stage `vendor/TSLPatcher` as a real submodule path in the repo.
- Files:
  - `vendor/TSLPatcher`
- Patterns to follow:
  - Existing vendor gitlink entries under `vendor/`
- Approach:
  - Add the submodule using the verified GitHub URL.
  - Keep the working tree isolated to the new path and avoid touching other vendor directories.
- Test scenarios:
  - `git ls-tree HEAD vendor/TSLPatcher` reports a gitlink after commit staging.
  - `git submodule status` includes `vendor/TSLPatcher`.
- Verification:
  - `git submodule status`
  - `git diff -- .gitmodules vendor/TSLPatcher`

### U3: Align vendor docs

- Goal: make the README describe the repo's vendor submodule layout truthfully.
- Files:
  - `README.md`
- Patterns to follow:
  - Existing vendor-submodule table in `README.md`
- Approach:
  - Move `vendor/TSLPatcher` into the submodule listing.
  - Ensure the section no longer claims the path is a checked-in vendor reference.
- Test scenarios:
  - README vendor section matches the resulting git submodule layout.
  - Clone/update instructions still read coherently after the change.
- Verification:
  - Review rendered markdown diff in `README.md`

## Risks and mitigations

- Risk: existing vendor gitlinks may point at upstreams that are not obvious from the empty working directories.
  Mitigation: derive URLs from repo-local references and only repair the mappings needed to make submodule commands usable again.

- Risk: the dirty worktree contains unrelated user changes.
  Mitigation: stage only the files touched by this slice and leave unrelated modifications untouched.

## Validation plan

1. Run `git submodule status` after metadata changes.
2. Inspect the focused diff for `.gitmodules`, `README.md`, and `vendor/TSLPatcher`.

## Status deltas

- Landed: plan created.
- Partial/uncertain: existing vendor gitlink URLs still need to be reconstructed from repo-local evidence.
- Next-step change: implementation will start with `.gitmodules`, then add `vendor/TSLPatcher`, then run a narrow submodule validation.
