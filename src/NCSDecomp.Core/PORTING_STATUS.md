# NCSDecomp.Core — DeNCS port status

Port target: `vendor/DeNCS` (Java). Library: **`src/NCSDecomp.Core`**. CLI: **`src/NCSDecomp.NET`**. UI: **`src/NCSDecomp.UI`**.

**Full per-policy file accounting (271 `src/main/java` sources + exceptions):** [`docs/NCS_DENCS_JAVA_ACCOUNTING.md`](../../docs/NCS_DENCS_JAVA_ACCOUNTING.md).

## Inventory (vs `vendor/DeNCS`)

- **~272** `.java` files under `src/main/java` map to **`NCSDecomp.Core`** (+ **`KPatcher.Core/Formats/NCS/Decompiler/Decoder.cs`** for `Decoder.java`), with renames (`Logger` → `NcsDecompLogger`, `Settings` → `NcsDecompSettings`, `Type` → `DecompType`, `AExpression` → `IAExpression`, etc.). The full `vendor/DeNCS` tree (including tests) is on the order of **~270+** Java sources; product parity is the **`src/main/java`** decompiler/compiler stack, not a line-for-line duplicate of every test harness line.
- **`TreeModelFactory.java`** — not ported (Swing-only); `NCSDecomp.UI` uses Avalonia, not Java tree models.
- **Java test** `vendor/DeNCS/src/test/java/.../DeNCSCLIRoundTripTest.java` — exhaustive vanilla NSS↔NCS using **external** `nwnnsscomp` for compile/recompile; **C# port** lives in **`tests/KPatcher.Tests`** as **`NCSDecompCliRoundTripTest`** (opt-in: **`RUN_NCSDECOMP_JAVA_ROUNDTRIP_SUITE=1`**, traits **`ExternalCompiler`**, **`DeNCSJavaParity`**). The **decompile** step uses managed **`RoundTripUtil.DecompileNcsToNssFile`** (not spawning Java).
- **Managed vanilla decompile round-trip** (no `nwnnsscomp`): **`VanillaNssManagedDecompileRoundTripTests`** — K1/TSL samples from **`Vanilla_KOTOR_Script_Source`**, compile → **`NCSManagedDecompiler.DecompileToNss`** → recompile → **`NcsRoundTripAssertHelpers.AssertNcsStructurallyEqual`** (thresholds tuned so CI passes with submodule; many vanilla files still fail KCompiler or parser before this pipeline).
- **Other NCS/NSS fixtures in `tests/KPatcher.Tests` (representative “~8 classes”):** `NCSFormatTests`, `NCSCompilerTests`, `NCSOptimizerTests`, `NCSInterpreterTests`, `NCSRoundtripTests`, `VanillaNSSCompileTests`, `NcsDecompNetStyleRoundTripTests`, `NcsManagedFullDecompileSmokeTests`, `NcsLexerSmokeTest`, `RoundTripUtilManagedCompareTests`, `NCSDecompSyntaxHighlighterTests`, `NcsAstOutlineTests`, plus opt-in **`NCSDecompCliRoundTripTest`**.

## Product policy (KPatcher)

- **NSS → NCS:** managed **`KCompiler.Core`**; no registry spoofing and no requirement on `nwnnsscomp.exe`.
- **NCS → NSS:** **`NCSDecomp.Core`** + **`NCSManagedDecompiler`** in **`KPatcher.Core`**.
- **`CompilerExecutionWrapper.CreateRegistrySpoofer`:** always **`NoOpRegistrySpoofer`**. **`WindowsRegistrySpoofer`** was removed; DeNCS HKLM spoof path is intentionally not a product feature.

## Done (representative)

- Lexer, `lexer.dat`, tokens, Parser + `parser.dat`; **`Lexer.PushBack`** matches Java (`text[acceptLength..]`).
- AST nodes (`A*` / `P*`), `Start`, `IAnalysis` / `AnalysisAdapter`.
- Analysis: `CallGraphBuilder`, `CallSiteAnalyzer`, `PrototypeEngine`, pruned adapters, `SCCUtil`.
- Stack / const / variable types, `DoTypes`, `NodeUtils`, path finder, `Utils/*` tree passes (**`SetPositions`**, **`SetDestinations`**, **`SetDeadCode`**, **`FlattenSub`**, **`DestroyParseTree`**).
- `ActionsData` + `IActionsData` (KPatcher) for `Decoder` ACTION lines.
- `NcsParsePipeline`: bytes → token stream → parse tree; KPatcher **`Decoder`** aligns 13-byte header with DeNCS lexer layout.
- **`scriptutils/*`**, **`scriptnode/*`**, **`MainPass.cs`**, **`FileDecompiler.cs`** (managed pipeline wired from **`NCSDecomp.NET`**).
- **`NcsDecompSettings`**, **`RoundTripUtil`**, optional external-compiler helpers (`CompilerUtil`, `KnownExternalCompilers`, `NwnnsscompConfig`, `ExternalCompilerProcess`) for hosts that still shell out.

## Parser / full decompile

- **Reduce vs `GoTo` order:** Java `push(goTo(n), newK(), …)` evaluates **`newK()` first** (pops RHS), then **`goTo`**. In C#, `Push(GoTo(n), NewK(), …)` would evaluate **`GoTo` first** (wrong stack top). The port uses locals: `AstNode r = NewK(); Push(GoTo(n), r, …);` for every reduce arm (see comment on the reduce `switch` in **`Parser.cs`**). Wrong order produced `InvalidCastException` in `New0` / other factories.
- **`NcsManagedFullDecompileSmokeTests`** and the rest of **`tests/KPatcher.Tests`** pass on the managed decode → lexer → parser → `FileDecompiler` path. Further bytecode edge cases may still surface `ParserException` from genuine grammar mismatches; add fixtures as they appear.
- Decoder-only path (**`NcsParsePipeline.DecodeToTokenStream`**) is used for managed round-trip checks in **`RoundTripUtil.CompareManagedRecompileToOriginalDecoderText`**.

## Resources

Embed under `Resources/`: `lexer.dat`, `parser.dat`, `k1_nwscript.nss`, `tsl_nwscript.nss` (from `vendor/DeNCS/src/main/resources/`).
