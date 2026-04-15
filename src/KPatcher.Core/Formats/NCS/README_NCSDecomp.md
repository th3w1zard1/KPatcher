# NCS / DeNCS merge layout

- **On-disk source of truth:** `Formats/NCS/` under KPatcher.Core (NSS compiler, `NCS` model, binary read/write).
- **Built into `KCompiler.Core`:** Everything under `Formats/NCS/**/*.cs` except:
  - `Compiler/NCSCompiler.cs` (KPatcher.Core only)
  - `Decompiler/NCSManagedDecompiler.cs` (KPatcher.Core only — references `NCSDecomp.Core`)
  That includes `Decompiler/Decoder.cs`, `Decompiler/NCSDecompiler.cs`, and `Decompiler/IActionsData.cs`.
- **`NCSDecomp.Core`:** DeNCS port (lexer, parser, analysis, stack, `MainPass`, `FileDecompiler`). References **`KCompiler.Core` only** (no `KPatcher.Core` project ref — avoids a circular dependency).
- **`KPatcher.Core`:** References **`NCSDecomp.Core`** and compiles **`NCSManagedDecompiler`**, which exposes full managed NCS->NSS for the patcher/tests.
- **`NCSDecomp.NET`:** CLI; no external compilers (`nwnnsscomp`, etc.) per project rules.

## Public APIs

| API | Assembly | Role |
|-----|----------|------|
| `NCSDecompiler.Decompile(...)` | KCompiler.Core | Decoder -> token string |
| `NCSManagedDecompiler.DecompileToNss(...)` | KPatcher.Core | Full pipeline -> `.nss` (needs embedded nwscript in NCSDecomp.Core) |
| `NcsParsePipeline`, `FileDecompiler`, `ActionsData` | NCSDecomp.Core | Library entry points |
| `KnownExternalCompilers`, `NwnnsscompConfig`, `HashUtil` | NCSDecomp.Core | Optional: fingerprint `nwnnsscomp.exe` / `ncsdis.exe` and build argv lists (DeNCS parity; not used by managed-only CLI) |
| `CompilerUtil`, `CompilerExecutionWrapper`, `ExternalCompilerProcess` | NCSDecomp.Core | Optional: discover `tools/`, prepare nwscript/includes, run external compiler (`CreateRegistrySpoofer` is always no-op; no HKLM spoofing) |
| `RoundTripUtil` | NCSDecomp.Core | Managed leg of round-trip: NCS->NSS with game flag; `GetRoundTripDecompiledCode` finds sibling `.ncs` |
| `NcsBytecodeCompare` | NCSDecomp.Core | Optional: NCS byte diff for diagnostics |
| `NwScriptSyntaxHighlighter` | NCSDecomp.Core | NSS syntax segments for UI (DeNCS `NWScriptSyntaxHighlighter.java`); used by `NCSDecomp.UI` |
| `NcsBytecodeSyntaxHighlighter` | NCSDecomp.Core | Decoder token-stream segments for UI (DeNCS `BytecodeSyntaxHighlighter.java`); used by `NCSDecomp.UI` |
| `NcsAstOutline` / `AstOutlineNode` | NCSDecomp.Core | Parse-tree outline for tooling (reflection on SableCC `Get*` accessors; caps size/depth); used by `NCSDecomp.UI` |
| `RoundTripUtil.CompareManagedRecompileToOriginalDecoderText` | NCSDecomp.Core | Managed NSS->NCS via `ManagedNwnnsscomp`, then decoder token equality vs original (header-agnostic) |

## Config file (CLI / UI)

`NCSDecomp.NET` and `NCSDecomp.UI` load **`config/ncsdecomp.conf`** next to the executable (Java-compatible keys; see `NcsDecompSettings`). Legacy **`dencs.conf`** in the same folder is accepted. Compiler paths use the same property names as Java (`nwnnsscomp Folder Path`, `nwnnsscomp Filename`, optional `nwnnsscomp Path`); resolve with **`CompilerUtil.GetCompilerPathFromSettings`** or **`NcsDecompSettings.GetResolvedCompilerPath()`**.

Embed under `src/NCSDecomp.Core/Resources/`: `lexer.dat`, `parser.dat`, `k1_nwscript.nss`, `tsl_nwscript.nss` (see `NCSDecomp.Core.csproj` conditional `EmbeddedResource`).

Port status: `src/NCSDecomp.Core/PORTING_STATUS.md`.
