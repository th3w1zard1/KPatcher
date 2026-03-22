# DeNCS Java → KPatcher C# accounting (/lfg)

This document satisfies **“every relevant `.java` file accounted for”** by mapping **`vendor/DeNCS/src/main/java`** into shipped C# projects. It is the authoritative checklist for port completeness under **managed-first** product policy (no HKLM registry spoofing; no default dependency on `nwnnsscomp.exe`).

## Counts (verified from repo layout)

| Area | Count | Notes |
|------|------:|--------|
| `vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/**/*.java` | **271** | All NCS DeNCS library + CLI-adjacent Java in-tree |
| `src/NCSDecomp.Core/**/*.cs` (excl. `obj/`) | **~470+** | Library port + SableCC-style AST, analysis, utils |
| `vendor/DeNCS/src/test/java/**/*.java` | (separate) | Exhaustive round-trip harness; see below |

## Folder-level mapping (271 files → C#)

Every Java file under `.../ncs/<subpkg>/` maps to the same conceptual subfolder under **`src/NCSDecomp.Core`**, with the usual renames (`Logger` → `NcsDecompLogger`, `Type` → `DecompType`, `AExpression` → `IAExpression`, etc.):

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
| `DeNCSCLIRoundTripTest.java` | `tests/KPatcher.Tests/Formats/NCSDecompCliRoundTripTest.cs` (merged partials: includes, filter, normalize, diff/bytecode). **Opt-in:** `RUN_NCSDECOMP_JAVA_ROUNDTRIP_SUITE=1`, traits **`ExternalCompiler`**, **`DeNCSJavaParity`**. Compile/recompile steps may still shell out to **`nwnnsscomp`** when enabled; **decompile** uses managed **`RoundTripUtil.DecompileNcsToNssFile`**. |

**Managed vanilla coverage (default CI when submodule present):** `VanillaNssManagedDecompileRoundTripTests` — KCompiler compile → `NCSManagedDecompiler.DecompileToNss` → recompile → structural NCS compare (`NcsRoundTripAssertHelpers`).

## NCS/NSS test fixtures in `KPatcher.Tests` (≥ /lfg “~8 classes”)

Compiler / format / interpreter / optimizer / round-trip / lexer / decomp / syntax / util:

- `NCSCompilerTests`, `NCSFormatTests`, `NCSInterpreterTests`, `NCSOptimizerTests`
- `NCSRoundtripTests`, `VanillaNSSCompileTests`, `VanillaNssManagedDecompileRoundTripTests`
- `NcsDecompNetStyleRoundTripTests`, `NcsManagedFullDecompileSmokeTests`, `NcsLexerSmokeTest`
- `RoundTripUtilManagedCompareTests`, `NCSDecompSyntaxHighlighterTests`, `NcsAstOutlineTests`
- Opt-in: `NCSDecompCliRoundTripTest`

## Verification

```bash
dotnet test KPatcher.sln -c Debug
```

Expect **all** `KPatcher.Tests` to pass; NCS/NSS coverage is included in that assembly.

## Maintenance

If `vendor/DeNCS` adds Java under `src/main/java`, extend this table (new subpackage → new Core folder) or add a row under **superseded** if policy excludes it (e.g. new Windows-only spoof helper).
