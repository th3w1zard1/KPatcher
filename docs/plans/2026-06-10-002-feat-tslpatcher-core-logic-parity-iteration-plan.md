---
title: "feat: TSLPatcher core logic parity iteration (living authority)"
type: feat
status: active
date: 2026-06-10
supersedes:
  - docs/plans/2026-06-10-001-feat-tslpatcher-core-logic-implementation-plan.md
  - docs/plans/2026-05-28-001-fix-tslpatcher-core-logic-parity-implementation-plan.md
  - docs/plans/2026-05-27-002-refactor-tslpatcher-core-logic-parity-audit-refresh-plan.md
origin: ".cursor/plans/tslpatcher_parity_7a30292f.plan.md"
---

# TSLPatcher Core Logic Parity Iteration (Living Plan)

**Authority:** This document is the single active TSLPatcher parity workstream plan. Older plans listed in `supersedes` are historical context only.

## Source baselines

| Source | Commit / ref | Remote drift |
|--------|----------------|--------------|
| **TSLPatcher** (`vendor/TSLPatcher`) | `88a191a66c9bd61eb9966b79efe66ea3f5c33c30` | Matches `origin` at `88a191a` (2026-06-10 fetch) |
| **KPatcher** (`master`) | HEAD at iteration start | Product parity target |

**TSLPatcher behavior-relevant tree (14 files):**

```
vendor/TSLPatcher/TSLPatcher.dpr
vendor/TSLPatcher/U2DAEdit.pas
vendor/TSLPatcher/UERFHandler.pas
vendor/TSLPatcher/UGFFFile.pas
vendor/TSLPatcher/UGFFHandler.pas
vendor/TSLPatcher/UMainForm.pas
vendor/TSLPatcher/UNamespaceForm.pas
vendor/TSLPatcher/USSFFile.pas
vendor/TSLPatcher/UST_Common.pas
vendor/TSLPatcher/UST_IniFile.pas
vendor/TSLPatcher/UStrTok.pas
vendor/TSLPatcher/UTLKFile.pas
vendor/TSLPatcher/UTSLPatcher.pas
vendor/TSLPatcher/UTSLPatcher12.pas
```

**KPatcher behavior surface:** `src/KPatcher.Core` (221 `.cs` files) — orchestration (`Patcher/ModInstaller.cs`), config (`Reader/ConfigReader.cs`, `Reader/NamespaceReader.cs`), memory (`Memory/PatcherMemory.cs`), mods (`Mods/*`), formats (`Formats/*`). UI workflow owners: `src/KPatcher.UI/Core.cs`, `ViewModels/MainWindowViewModel.cs`.

## Behavior-owner mapping

| TSLPatcher unit | Responsibility | KPatcher owners |
|-----------------|----------------|-----------------|
| `TSLPatcher.dpr` | App entry, default `changes.ini`/`info.rtf` | `KPatcher.UI/Program.cs`, `PatcherNamespace` defaults |
| `UST_Common.pas` | Shared parse/utils, ResRef, backup, writable | `ConfigReader`, `ResRef`, `ModInstaller`, `SystemHelpers` |
| `UST_IniFile.pas` | INI `<#LF#>`/`<#CR#>` token expansion | `ConfigReader.NormalizeTslPatcherCRLF` |
| `UStrTok.pas` | String tokenizer (unused in `.dpr`) | Ad hoc `Split` in Core (open) |
| `U2DAEdit.pas` | 2DA V2.b CRUD | `Formats/TwoDA/*`, `Mods/TwoDA/*` |
| `UGFFFile.pas` | GFF V3.2 tree patch model | `Formats/GFF/*`, `Mods/GFF/*` |
| `UGFFHandler.pas` | Legacy GFF (superseded) | N/A — parity target is `UGFFFile` |
| `UTLKFile.pas` | TLK V3.0 + append merge | `Formats/TLK/*`, `Mods/TLK/*` |
| `UERFHandler.pas` | ERF/RIM/MOD capsules | `Formats/ERF/*`, `Formats/RIM/*`, `Capsule/*` |
| `USSFFile.pas` | SSF V1.1 (40 slots) | `Formats/SSF/*`, `Mods/SSF/*` |
| `UTSLPatcher.pas` | Install pipeline orchestration | `ModInstaller`, `ConfigReader` |
| `UTSLPatcher12.pas` | Older pipeline order (historical) | Not a parity target |
| `UMainForm.pas` | GUI install workflow | `MainWindowViewModel`, `Core` |
| `UNamespaceForm.pas` | `namespaces.ini` selection | `NamespaceReader`, `Core` namespace resolution |

## Comparison ledger

**Progress: `[##############] 14/14 TSLPatcher files compared`**

| # | Pascal file | Parity status | Notes |
|---|-------------|---------------|-------|
| 1 | `TSLPatcher.dpr` | Intentional | Avalonia + CLI extensions; same default ini/rtf names |
| 2 | `UST_Common.pas` | Mostly same | `ParseIntValue('4294967295')` → `-1` landed; ResRef filter-vs-throw and install-time writable clearing remain open |
| 3 | `UST_IniFile.pas` | Mostly same (read) | CRLF tokens on patch values; Settings strings not globally expanded |
| 4 | `UStrTok.pas` | Open | Not referenced from `.dpr`; low install impact |
| 5 | `U2DAEdit.pas` | Fixed pass 2 | INI-order apply in `Modifications2DA.Apply` (was grouped reorder) |
| 6 | `UGFFFile.pas` | Mostly same | Intentional UInt64/VOID support beyond Pascal |
| 7 | `UGFFHandler.pas` | N/A | Superseded by `UGFFFile` |
| 8 | `UTLKFile.pas` | Fixed this iteration | Append dedup now reuses matching dialog entries (was mismatch) |
| 9 | `UERFHandler.pas` | Same / intentional arch | In-memory capsules vs lazy streams |
| 10 | `USSFFile.pas` | Same | 40 slots, labels, token resolution |
| 11 | `UTSLPatcher.pas` | Partial | Binary-verified pipeline order in `ModInstaller`; NCS-only HACK + managed compile intentional |
| 12 | `UTSLPatcher12.pas` | N/A | Historical only |
| 13 | `UMainForm.pas` | Mostly aligned | Install path resolution fixed to match preview fallback |
| 14 | `UNamespaceForm.pas` | Mostly aligned | Defaults + `..` confinement landed; install now uses `ResolveInstallPaths` |

### Batch comparison log

1. **Batch 1 (shared utils):** `UST_Common`, `UST_IniFile`, `UStrTok`, `TSLPatcher.dpr` — compared 2026-06-10.
2. **Batch 2 (formats):** `U2DAEdit`, `UGFFFile`, `UGFFHandler`, `UTLKFile`, `UERFHandler`, `USSFFile` — compared 2026-06-10.
3. **Batch 3 (orchestration):** `UTSLPatcher`, `UTSLPatcher12`, `UMainForm`, `UNamespaceForm` — compared 2026-06-10.

## Three-delta (living)

### Landed

- Full 14-file inventory and behavior-owner map (this document).
- Comparison ledger with progress bar at 14/14.
- **Install path parity:** `Core.ResolveInstallPaths` + `InstallMod` / `ValidateConfig` / `MainWindowViewModel` use namespace fallback + localized resolution at install time (not only preview).
- **TLK append dedup:** `ModifyTLK.Apply` reuses existing dialog entries with matching text+sound (TSLPatcher `AppendTLKData` behavior).
- **2DA modifier INI order:** `Modifications2DA.Apply` iterates `Modifiers` in load order (UTSLPatcher `2DAList` loop parity).
- **`SafeStrToInt` sentinel:** `ParseIntValue("4294967295")` returns `-1` for GFF Delay-style Int32 fields.
- Characterization tests: `CoreNamespaceInstallPathTests`, `TlkModificationTests.Apply_Append_ReusesExistingIdenticalEntry`, `TwoDaModifierOrderTests`, `GFF_ModifyField_UInt32MaxDecimal_ShouldParseAsNegativeOneForInt32`.

### Partial / uncertain

- **Pipeline authority** remains binary-verified order, not newer `UTSLPatcher.pas` source order (documented in audit).
- **HACKList** NCS-only narrowing and **managed CompileList** remain intentional product choices.
- **ResRef** filter-vs-throw and **install-time writable** clearing remain open (`UST_Common`).

### Next

- Golden tests for additional interleaved 2DA INI corpora if regressions appear.
- ResRef sanitization parity vs `StringToResRef` if mod corpus hits invalid chars.

## Fixes applied this iteration

| Delta | Fix | Tests |
|-------|-----|-------|
| Preview vs install namespace path | `Core.ResolveInstallPaths` | `CoreNamespaceInstallPathTests` |
| TLK append duplicate StrRefs | Reuse matching entry in `ModifyTLK.Apply` | `Apply_Append_ReusesExistingIdenticalEntry` |
| 2DA modifier grouped reorder | Apply `Modifiers` in INI load order | `TwoDaModifierOrderTests` |
| `SafeStrToInt` decimal max | `ParseIntValue("4294967295")` → `-1` | `GFF_ModifyField_UInt32MaxDecimal_*` |

## Validation

- Targeted: namespace install path, TLK dedup, existing `ModInstaller` / `CoreNamespaceFallback` tests.
- Full: `./scripts/dotnet-test.sh KPatcher.sln -c Debug` (wrapped, ≤600s).

## Policy reminders

- Managed NSS/NCS only (`KCompiler`, `NCSDecomp.Core`); no `nwnnsscomp.exe` in product paths.
- C# 7.3 max; ephemeral test fixtures only.
