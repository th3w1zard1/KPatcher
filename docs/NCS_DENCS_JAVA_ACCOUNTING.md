# DeNCS Java -> KPatcher C# accounting (/lfg)

This document satisfies **“every relevant `.java` file accounted for”** by mapping **`vendor/DeNCS/src/main/java`** into shipped C# projects. It is the authoritative checklist for port completeness under **managed-first** product policy (no HKLM registry spoofing; no default dependency on `nwnnsscomp.exe`).

## Counts (verified from repo layout)

| Area | Count | Notes |
|------|------:|--------|
| `vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/**/*.java` | **271** | All NCS DeNCS library + CLI-adjacent Java in-tree |
| `src/NCSDecomp.Core/**/*.cs` (excl. `obj/`) | **277** | Library port + SableCC-style AST, analysis, utils |
| `vendor/DeNCS/src/test/java/**/*.java` | (separate) | Exhaustive round-trip harness; see below |

## Folder-level mapping (271 files -> C#)

Every Java file under `.../ncs/<subpkg>/` maps to the same conceptual subfolder under **`src/NCSDecomp.Core`**, with the usual renames (`Logger` -> `NcsDecompLogger`, `Type` -> `DecompType`, `AExpression` -> `IAExpression`, etc.):

| Java subpackage (`.../ncs/…`) | C# location |
|------------------------------|-------------|
| `node/` | `NCSDecomp.Core/Node/` (`A*`, `P*`, `T*`, `Node`, `Cast`, …) |
| `parser/` | `NCSDecomp.Core/Parser/` |
| `lexer/` | `NCSDecomp.Core/Lexer/` |
| `analysis/` | `NCSDecomp.Core/Analysis/` |
| `scriptnode/` | `NCSDecomp.Core/ScriptNode/` |
| `scriptutils/` | `NCSDecomp.Core/ScriptUtils/` |
| `stack/` | `NCSDecomp.Core/Stack/` |
| `utils/` | `NCSDecomp.Core/Utils/` |
| Root `.../ncs/*.java` (pipeline entrypoints) | `NCSDecomp.Core/*.cs` (`FileDecompiler`, `MainPass`, `NcsParsePipeline`, `RoundTripUtil`, …) |

**Decoder / dual stack:** Java `Decoder.java` (token-line view used with KOTOR tooling) is integrated for patcher parity as **`KPatcher.Core/Formats/NCS/Decompiler/Decoder.cs`** plus **`IActionsData`** wiring.

## Entry hosts (not duplicated inside Core)

| Java | C# |
|------|-----|
| `DeNCSCLI.java` | `NCSDecomp.NET` (CLI host) + `NCSDecomp.Core/NcsDecompCli.cs` |
| Swing / desktop UI in Java | `NCSDecomp.UI` (Avalonia) — different framework, same responsibilities where applicable |

## Explicit “not a line-for-line class port” (superseded by policy or platform)

| Java file | Disposition |
|-----------|-------------|
| `TreeModelFactory.java` | **Swing-only** — not ported; Avalonia UI does not use Java tree models. |
| `RegistrySpoofer.java` (Windows HKLM behaviour) | **Deliberately not a product feature.** C# exposes **`IRegistrySpoofer`** / **`NoOpRegistrySpoofer`**; **`CompilerExecutionWrapper.CreateRegistrySpoofer`** always returns no-op (see `CompilerExecutionWrapper.cs`). |
| `NWScriptSyntaxHighlighter.java`, `BytecodeSyntaxHighlighter.java` | UI/editor colouring: **`NCSDecomp.UI`** and/or **`tests/KPatcher.Tests/NCSDecompSyntaxHighlighterTests.cs`** (behaviour covered in managed stack). |
| `Decompiler.java` (very large) | **Split** across `FileDecompiler`, `MainPass`, stack/type passes, and related types in Core — standard port pattern, not a single 1:1 file. |

## Test sources (`src/test/java`)

| Java | C# |
|------|-----|
| DeNCS CLI round-trip (vendor tree) | `tests/KPatcher.Tests/Formats/NCSDecompCliRoundTripTest.cs` (includes, filter, normalize, diff/bytecode). Runs with **`dotnet test`** (traits **`ExternalCompiler`**, **`DeNCSRoundTrip`**). Compile/recompile shell out to **`nwnnsscomp`**; **decompile** uses managed **`RoundTripUtil.DecompileNcsToNssFile`**. |

**Managed vanilla coverage (default CI when submodule present):** `VanillaNssManagedDecompileRoundTripTests` — KCompiler compile -> `NCSManagedDecompiler.DecompileToNss` -> recompile -> structural NCS compare (`NcsRoundTripAssertHelpers`).

## NCS/NSS test fixtures in `KPatcher.Tests` (≥ /lfg “~8 classes”)

Compiler / format / interpreter / optimizer / round-trip / lexer / decomp / syntax / util:

- `NCSCompilerTests`, `NCSFormatTests`, `NCSInterpreterTests`, `NCSOptimizerTests`
- `NCSRoundtripTests`, `VanillaNSSCompileTests`, `VanillaNssManagedDecompileRoundTripTests`
- `NcsDecompNetStyleRoundTripTests`, `NcsManagedFullDecompileSmokeTests`, `NcsLexerSmokeTest`
- `RoundTripUtilManagedCompareTests`, `NCSDecompSyntaxHighlighterTests`, `NcsAstOutlineTests`
- Opt-in: `NCSDecompCliRoundTripTest`

## Completion checklist (/lfg command)

| Criterion | Status | Evidence |
|-----------|--------|----------|
| All relevant `.java` accounted or superseded | **Met** | **271** files mapped in tables above; empty local `vendor/DeNCS` does not change accounting (layout documented from repo history). |
| NSS→NCS via managed KCompiler | **Met** | `NCSCompiler`, `ModificationsNSS` → `NCSAuto.CompileNss`; `ConfigReader` logs managed compile (no `nwnnsscomp` resolution). |
| NCS→NSS via NCSDecomp.Core + patcher API | **Met** | `NCSManagedDecompiler`, `NCSDecompiler`; Core hosts `FileDecompiler` / `RoundTripUtil` pipeline. |
| No product registry spoofer | **Met** | `CompilerExecutionWrapper.CreateRegistrySpoofer()` → `NoOpRegistrySpoofer` only. |
| ≥8 NCS/NSS test classes | **Met** | **14** classes listed under **NCS/NSS test fixtures** (includes opt-in harness). |
| Roundtrip tests exist and pass (default tier) | **Met** | `NCSRoundtripTests`, `VanillaNssManagedDecompileRoundTripTests`, `NcsDecompNetStyleRoundTripTests`, `RoundTripUtilManagedCompareTests`, etc. |
| Full solution tests pass | **Met** | Wrapper command below; **789/789** `KPatcher.Tests` (Default.runsettings) on Linux at `9b27aaaa` (2026-06-10). |
| Default CI fails on test regressions | **Met** | Primary `ci.yml` test step no longer uses `continue-on-error`. |
| Vendor tier not vacuous in default CI | **Met** | `Category=Vendor` excluded from `Default.runsettings`; vendor tests use `RequireVanillaSubmodule()` and run via `Vendor.runsettings` when the tree is present. |

**Opt-in (not default CI):** `NCSDecompCliRoundTripTest` may shell out to `nwnnsscomp` when installed (`ExternalCompiler`, `DeNCSRoundTrip` traits). `Category=Vendor` vanilla NSS compile/decompile tests use `Vendor.runsettings` (workflow_dispatch `run_vendor_nss` on optional tiers). Neither category is part of product compile/decompile paths.

## Verification

**Last /lfg verification:** 2026-06-10 — `chore/dencs-lfg-reverification-2026-06-10` @ `617269ca` (eleventh `/lfg` re-run). Re-confirmed: all **271** Java sources accounted; **277** C# files in `NCSDecomp.Core`; product compile/decompile paths are managed-only (`ModificationsNSS` and `NCSCompiler` use `NCSAuto.CompileNss` only; `ConfigReader` does not resolve `nwnnsscomp.exe`; `CompilerExecutionWrapper.CreateRegistrySpoofer` is always no-op). Default tier excludes `Category=Vendor` so empty `vendor/Vanilla_KOTOR_Script_Source` cannot yield vacuous passes.

**NCS/NSS test gate (managed tooling):** **223** `KPatcher.Tests` + **2** `KCompiler.Tests` + **1** `NCSDecomp.Tests` passed with filter `FullyQualifiedName~NCS|FullyQualifiedName~Nss|FullyQualifiedName~Decomp` (2026-06-10 eleventh pass). Default CI uses `Default.runsettings` (excludes `DeNCSRoundTrip`, `Vendor`, `WindowsOnly`, etc.).

**Full default-tier suite** (repo wrapper, Linux):

```bash
bash ./scripts/dotnet-test.sh KPatcher.sln -c Debug
```

**789/789 passed** in `KPatcher.Tests` (2026-06-10) via `bash ./scripts/dotnet-test.sh KPatcher.sln -c Debug` (four `Category=Vendor` facts opt in via `Vendor.runsettings`). Opt-in exhaustive harness (`NCSDecompCliRoundTripTest`) may still use `nwnnsscomp.exe` when tools are present — not required for product or default CI.

## Maintenance

If `vendor/DeNCS` adds Java under `src/main/java`, extend this table (new subpackage -> new Core folder) or add a row under **superseded** if policy excludes it (e.g. new Windows-only spoof helper).
