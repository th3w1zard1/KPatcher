---
title: "feat: DeNCS /lfg CI gate and vendor test truthfulness"
type: feat
status: completed
date: 2026-05-23
origin: docs/NCS_DENCS_JAVA_ACCOUNTING.md
---

# feat: DeNCS /lfg CI gate and vendor test truthfulness

## Summary

Eighth `/lfg` pass after `ce-repo-research-analyst` found drift: default CI runs `Category=Vendor` tests that **pass vacuously** when `vendor/Vanilla_KOTOR_Script_Source` is empty, and `ci.yml` uses `continue-on-error: true` on the primary test step. Port criteria remain met; this plan hardens automated sign-off.

## Requirements

| ID | Requirement |
|----|-------------|
| R1 | Exclude `Category=Vendor` from `Default.runsettings` |
| R2 | Add `Vendor.runsettings` for optional vendor-tier runs |
| R3 | Vendor-tier tests fail clearly when submodule absent (no silent `return`) |
| R4 | Remove `continue-on-error` from primary `ci.yml` test job |
| R5 | Re-run wrapper; update accounting doc |

## Scope

- Do not change `test-builds.yml` / `dotnet-desktop.yml` (matrix publish jobs; documented platform caveats).
- No new Java porting.

## Implementation units

U1: runsettings + Vendor.runsettings  
U2: Vanilla test guards  
U3: ci.yml gate  
U4: doc stamp
