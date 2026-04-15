---
title: "feat: Exhaustive diagnostic logging for compiler, decompiler, and KEditChanges toolchains"
type: feat
status: active
date: 2026-03-21
---

# ✨ feat: Exhaustive diagnostic logging for compiler, decompiler, and KEditChanges toolchains

## Overview

Add **systematic, level-gated, diagnostically useful** logging across all `src/` projects **except** `KPatcher.Core` and `KPatcher.UI`. In scope:

| Project / area | Role |
|----------------|------|
| `KCompiler.Core`, `KCompiler.NET` | Managed NSS->NCS pipeline and `kcompiler` CLI |
| `NCSDecomp.Core`, `NCSDecomp.NET`, `NCSDecomp.UI` | DeNCS port, `NCSDecompCLI`, Avalonia UI host |
| `KEditChanges`, `KEditChanges.NET` | Library placeholder + `keditchanges-cli` umbrella |

**Out of scope (explicit):** `KPatcher.Core`, `KPatcher.UI` (install log, GUI robust logger, `PatchLogger` / `InstallLogWriter` parity remain unchanged unless a separate issue covers them).

**Goal:** Make failures **actionable** (what phase, which paths, which options, how long), make “successful but wrong” decompiler paths **visible** at trace levels, and keep **default** behavior **quiet and fast** with `Log.IsEnabled` short-circuits on hot paths.

---

## Problem statement / motivation

Validated and inferred gaps (see structured findings from bug-reproduction / code review):

1. **CLI exception handling** — `KCompiler.NET`, `KEditChanges.NET` (`RunKCompiler`), and `NcsDecompCli` use `Console.Error.WriteLine("Error: " + ex.Message)`, which drops stack traces, inner exceptions, and fields like `FileNotFoundException.FileName` (e.g. missing NSS shows a generic message without the resolved path).
2. **Compiler diagnostics not surfaced** — `ManagedNwnnsscomp` passes `errorlog: null` into `NCSAuto.CompileNss`, so multi-line compiler feedback may never reach the CLI.
3. **Decompiler observability split-brain** — `NcsDecompLogger` + `-v` exist for CLI, but `SubScriptLogger` and much of `SubScriptState` use `Debug.WriteLine` only; many `FileDecompiler` passes use empty `catch` blocks and continue with **no** user-visible signal.
4. **Inconsistent channels** — Mix of stdout (`Console.WriteLine` in libraries), stderr (`NcsDecompLogger`), and silent fallbacks (e.g. invalid encoding name in `NcsDecompCli.ResolveOutputEncoding` -> UTF-8 without warning).
5. **Umbrella CLI** — `keditchanges-cli` delegates to the same runners without a shared **correlation** story for support logs.

Institutional context: `docs/solutions/debugging-patterns/` documents **lexer/parser parity** debugging for NCSDecomp, not a general logging contract. `docs/PERFORMANCE_TESTING.md` covers **test** verbosity and profiling — complementary to runtime tool logging.

---

## Research decision

**Skipped external framework research.** The repo already exposes concrete gaps; alignment with **Microsoft.Extensions.Logging** (or a thin shared façade) is standard .NET practice and is noted below as the recommended direction. No payment/security/external-API risk warrants mandatory web research for this plan.

### Consolidated internal references

- `src/NCSDecomp.Core/Utils/NcsDecompLogger.cs` — levels, ANSI, stderr default.
- `src/NCSDecomp.Core/NcsDecompCli.cs` — `-v`, encoding fallback, success stdout vs verbose stderr.
- `src/NCSDecomp.Core/FileDecompiler.cs` — swallowed exceptions per analysis pass.
- `src/NCSDecomp.Core/ScriptUtils/SubScriptLogger.cs`, `SubScriptState.cs` — `Debug`-only trace volume.
- `src/KCompiler.NET/Program.cs`, `src/KEditChanges.NET/Program.cs` — CLI catch blocks, `RunKCompiler`.
- `src/KCompiler.Core/ManagedNwnnsscomp.cs` — `errorlog: null`.
- `src/NCSDecomp.UI/Program.cs` — `LogToTrace()` Avalonia hook.

---

## Proposed solution (high level)

### 1. Shared abstraction (choose one primary path)

**Option A (recommended):** Add **`Microsoft.Extensions.Logging.Abstractions`** to `KCompiler.Core`, `NCSDecomp.Core`, and (as needed) `KEditChanges`. CLI hosts (`KCompiler.NET`, `NCSDecomp.NET`, `KEditChanges.NET`) configure **`Microsoft.Extensions.Logging.Console`** (or a minimal custom formatter) with:

- **stderr** for diagnostics (Information and below for “human” progress; Error/Warn always appropriate).
- **stdout** reserved for **primary tool output** (e.g. `Wrote NSS: …`, machine-readable lines) — or document a breaking cleanup to move diagnostics fully off stdout over a release.

**Option B (lighter):** Introduce a small **`KPatcher.Tools.Diagnostics`**-style shared project (or shared static façade) wrapping `TextWriter`/callbacks, mirroring `NcsDecompLogger` semantics across all tools — **without** new NuGet deps. Higher maintenance, fewer ecosystem integrations.

**Decision for implementers:** Prefer **Option A** unless package footprint in `KCompiler.Core` / `NCSDecomp.Core` is rejected; then fall back to Option B with the same **level** and **event id** conventions.

### 2. Default provider = no-op when host does not configure logging

Libraries must use **`ILogger<T>`** or `NullLogger` by default so **KPatcher** (excluded) and other hosts are not spammed until they opt in. CLI entry points **always** attach a real factory.

### 3. Verbosity control

- **CLI:** Where nwnnsscomp-style argv is crowded, prefer **`--verbose` / `-v`** mapping to `LogLevel.Information` and **`--diagnostic` / env** (`KPATCHER_TOOL_LOG_LEVEL`, `DOTNET_LOGGING_*` pattern) for `Debug`/`Trace`.
- **Document** the matrix in `README.md` or `docs/` for each tool (user did not request doc edits in implementation — add only if the plan’s implementer includes docs in scope).

### 4. Structured context (minimum fields)

For each **phase boundary** and **exception**, log (when level permits):

- **Tool** (e.g. `kcompiler`, `NCSDecompCLI`, `keditchanges-cli`), **version**, **RID** (optional).
- **Operation** (`compile`, `decomp`, `cli.parse`, `ui.decompile`, …).
- **Phase** (stable names; see table below).
- **CorrelationId** — one per `keditchanges-cli` process, propagated into delegated runners.
- **ElapsedMs** — phase duration.
- **Paths** — **redacted by default** (basename + optional hash of full path); full path only at Debug+ behind env flag (e.g. `KPATCHER_LOG_FULL_PATHS=1`).
- **Game / `-g`**, **encoding** (when relevant), **byte length** of inputs.

**Do not** log NSS/NCS/script **content** at default levels; trace content dumps require explicit opt-in and max length caps.

---

## Diagnostic priorities (symptom -> instrument -> fields)

Use this when deciding **where** logging pays off first (“meticulous” means **high-signal**, not **high-volume**).

| Symptom / question | Best place to log | Minimum useful fields |
|--------------------|-------------------|------------------------|
| “Compile failed with one vague line” | CLI top-level `catch` + `ManagedNwnnsscomp` | Exception type, `Message`, `FileNotFoundException.FileName`, inner chain, **phase** (`io.read_nss` vs `compile.parse`), redacted paths, `game`, correlation id |
| “Compiler said nothing but NSS is wrong” | `NCSAuto.CompileNss` / `errorlog` sink | Compiler diagnostic lines, include path resolution hints (not full file contents) |
| “Decomp succeeded but NSS is wrong” | `FileDecompiler` per-pass `catch` | Pass name, whether analysis was skipped, `ex.Message` at Debug, **Warning** if output may be degraded |
| “Wrong encoding / mojibake in NSS” | `NcsDecompCli.ResolveOutputEncoding` | Requested encoding name, **fallback to UTF-8** (always warn), output path redacted |
| “Works in debugger, silent in CLI” | `SubScriptLogger`, `Debug.WriteLine` call sites | Route to `ILogger` at **Trace**; gate with `IsEnabled(LogLevel.Trace)` |
| “Which tool/version ran?” | Host `Program` + first library entry | Tool id, assembly informational version, optional RID |
| “Umbrella CLI; can’t correlate logs” | `KEditChanges.NET` dispatch | Single **CorrelationId** minted once, passed into `RunKCompiler` / `NcsDecompCli` |
| “IO flake (locked file, disk full)” | Read/write helpers | Operation, redacted path, expected vs actual bytes, offset, HRESULT/OS hint if available |

**Rule of thumb:** Log at **boundaries** (parse/IO/pipeline phase changes), **decisions** (fallback chosen), and **failures** (including swallowed passes). Avoid logging **inside** inner loops unless **Trace** and explicitly enabled.

---

## Where to log (diagnostic map)

### KCompiler.Core

| Location | What to log | Level |
|----------|-------------|--------|
| `ManagedNwnnsscomp` (entry/exit) | Resolved input/output paths (redacted), game, `nwscriptPath` presence, `debug` flag | Information |
| `NCSAuto.CompileNss` call site | Start/end, duration; wire **`errorlog`** to logger or `StringBuilder` flushed on Warning+ | Warning on diagnostic lines |
| File IO (read NSS, write NCS) | Open failure, locks, bytes written, encoding | Warning / Error |
| Optimizer / pipeline stages | Stage name + duration; skip only if truly negligible | Debug |

### KCompiler.NET / KEditChanges.NET (`RunKCompiler`)

| Location | What to log | Level |
|----------|-------------|--------|
| Arg parse success | Normalized map (no secrets), output paths | Debug |
| Arg parse failure | Parser message + raw argv count | Warning |
| Top-level `catch` | **Full diagnostic**: `ex.ToString()` or structured exception (type, message, inner), **`FileNotFoundException.FileName`**, correlation id | Error |
| Success | One Information line: output path (redacted), size, duration | Information (optional; may stay silent for parity) |

### NCSDecomp.Core

| Location | What to log | Level |
|----------|-------------|--------|
| `NcsDecompCli` | Encoding fallback (**explicit Warning**), invalid args, input size, game | Warning / Information |
| `FileDecompiler` | **Each major pass**: start/end/duration; on `catch` — pass name + exception message (**not** full stack at Info) | Debug; Warning when pass skipped changes behavior |
| `SubScriptState` / `SubScriptLogger` | Route to same logger as CLI at Trace; remove `Debug`-only exclusivity for verbose runs | Trace |
| `MainPass`, `NameGenerator`, other `Debug.WriteLine` | Migrate to `ILogger` Trace or remove | Trace |
| Library `Console.WriteLine` (`FileDecompiler`, `DoTypes`, `SubroutineState`, `PrototypeEngine`, …) | Replace with logger; default **no** stdout from library | Information/Debug |

### NCSDecomp.NET

| Location | What to log | Level |
|----------|-------------|--------|
| `Program` | Host startup, forwarding args (Debug), exit code | Debug / Information |

### NCSDecomp.UI

| Location | What to log | Level |
|----------|-------------|--------|
| `Program` | Factory registration, log file path if file sink enabled | Information |
| Decompile command handler | Same phases as CLI where code is shared; UI errors to logger + existing user message | Error |
| Avoid | Modal per log line; optional “Open log” only if file sink exists |

### KEditChanges / KEditChanges.NET

| Location | What to log | Level |
|----------|-------------|--------|
| Root dispatch | Verb chosen, correlation id minted | Information |
| `info` / future subcommands | Duration, outcome | Debug |

---

## Phase naming (stable for tests and support)

**Compile:** `cli.parse` -> `io.read_nss` -> `compile.parse` -> `compile.codegen` -> `optimize` -> `io.write_ncs` -> `done`

**Decomp:** `cli.parse` -> `io.read_ncs` -> `decomp.decode` -> `decomp.analysis` -> `decomp.ast` -> `decomp.print` -> `io.write_nss` -> `done`

Implementers may subdivide `decomp.analysis` to match `FileDecompiler` passes; **document** the final enum or constants in one shared file per solution area.

---

## Acceptance criteria

- [ ] **AC-default:** With no flags/env, only **Error** (and critical **Warning** such as encoding fallback / skipped decomp pass) emit; no per-token/per-instruction logs; hot paths use `IsEnabled` checks.
- [ ] **AC-levels:** Published ladder (Error -> Warning -> Information -> Debug -> Trace) and what each adds for compile and decomp.
- [ ] **AC-structure:** Phase logs include **Tool**, **Operation**, **Phase**, **CorrelationId** (where applicable), **ElapsedMs**; stable **event ids** or category names for tests.
- [ ] **AC-paths:** Default redacted paths; full paths behind env flag only.
- [ ] **AC-no-content:** No NSS/NCS body in logs at Information or below.
- [ ] **AC-compile-diagnostics:** `errorlog` (or equivalent) from managed compile is visible at Warning+ or when `-v` compile is set.
- [ ] **AC-exceptions:** CLI failures log **actionable** detail (`FileName`, inner exceptions, optional full stack at Debug or env `KPATCHER_TOOL_LOG_STACK=1`).
- [ ] **AC-channels:** Library code does not write ungated stdout; document any intentional stdout lines for backward compatibility.
- [ ] **AC-keditchanges:** Single correlation id for umbrella invocations, passed through to `kcompiler` / `NCSDecompCLI` paths.
- [ ] **AC-ui:** `NCSDecomp.UI` can enable file or trace logging without breaking UX; default remains quiet.
- [ ] **AC-host:** When referenced from non-CLI hosts without configuration, logging is no-op (no surprise console noise).
- [ ] **AC-tests:** In-memory logger or `Microsoft.Extensions.Logging.Testing` asserts on key events for: missing file, invalid args, encoding fallback, skipped `FileDecompiler` pass (mocked failure); deterministic redaction in assertions.

---

## Dependencies and risks

| Risk | Mitigation |
|------|------------|
| **Stdout breakage** for scripts parsing `Wrote NSS:` | Keep primary success line stable; add stderr diagnostics only first; semver note if stdout is later cleaned. |
| **Log volume / perf** in `SubScriptState` | Trace-only + `IsEnabled`; optional sampling constants. |
| **PII in paths** | Redaction + env for full paths. |
| **Package references** in Core libs | Abstractions-only package; console sink only in executables. |
| **Test flakiness** | Fixed clock interface for ElapsedMs in tests; normalize paths in assertions. |

---

## Implementation phases

1. **Foundation** — [x] Add `Microsoft.Extensions.Logging.Abstractions` to `KCompiler.Core` / `NCSDecomp.Core`; phase name constants (`CompilePhaseNames`, `DecompPhaseNames`); optional `ILogger` on `ManagedNwnnsscomp` / `FileDecompiler` / `NcsDecompCli.Run`; CLI hosts use `Microsoft.Extensions.Logging` + `Console` with `KPATCHER_TOOL_LOG_LEVEL` default **Warning**.
2. **CLI hardening** — [x] Structured `LogError` on kcompiler / keditchanges compile paths with `ToolExceptionFormatter`; [x] `KPATCHER_CORRELATION_ID` minted in `keditchanges-cli` for compile/decomp delegations; [ ] Full `NCSAuto` / `errorlog` sink (still not implemented in shared compiler — tracked separately).
3. **NCSDecomp.Core** — [x] `FileDecompiler` warnings for swallowed analysis passes; [x] encoding fallback **Warning** in `ResolveOutputEncoding`; [x] `ExternalCompilerProcess` debug timings; [ ] Broad `SubScriptLogger` / `Debug.WriteLine` migration (deferred).
4. **NCSDecomp.UI** — [x] Console logger factory at startup + `MainWindow` diagnostics on decompile / AST / errors.
5. **KEditChanges** — [x] Umbrella dispatch logging; library remains placeholder-only.
6. **Tests and docs** — [x] `ToolDiagnosticsTests` (path redaction + exception formatting); [ ] Dedicated `docs/TOOL_LOGGING.md` (optional).

---

## Success metrics

- Support requests can answer “which phase failed” from logs without reproducer.
- CI failure artifacts include stderr sufficient to distinguish IO vs parse vs codegen.
- Benchmark: default-off logging adds **negligible** overhead (e.g. &lt; 1–2% on representative compile/decomp fixtures — tune in implementation).

---

## References

- `src/NCSDecomp.Core/Utils/NcsDecompLogger.cs`
- `src/NCSDecomp.Core/NcsDecompCli.cs`
- `src/NCSDecomp.Core/FileDecompiler.cs`
- `src/KCompiler.Core/ManagedNwnnsscomp.cs`
- `src/KCompiler.NET/Program.cs`
- `src/KEditChanges.NET/Program.cs`
- `docs/solutions/debugging-patterns/ncsdecomp-lexer-pushback-java-parity.md`
- `docs/solutions/debugging-patterns/ncsdecomp-parser-reduce-before-goto-csharp.md`
- `docs/PERFORMANCE_TESTING.md` (test verbosity / profiling, complementary)

---

## Open questions (resolve before or during implementation)

1. **Stdout policy:** Keep legacy stdout success lines indefinitely vs migrate diagnostics-only stderr in a major version?
2. **Stack traces:** Always with Error, or env-gated for user-facing CLIs?
3. **JSON lines:** Optional `--log-format json` for CI, or stderr text only for v1?

---

## Subagent / validation notes

- **Repo research:** Identified `NcsDecompLogger`, `SubScriptLogger`, `Console`/`Debug` split, no M.E.Logging in scoped projects today.
- **Bug-reproduction-validator:** Confirmed weak `ex.Message`-only CLI errors and missing path detail for missing NSS; identified `errorlog: null` and silent `FileDecompiler` catches as high-value logging targets. Source re-check: `KCompiler.NET` / `KEditChanges.NET` catch blocks still message-only; `ManagedNwnnsscomp` still `errorlog: null`; `NcsDecompCli.ResolveOutputEncoding` catch still silent.
- **Spec-flow-analyzer:** Supplied AC gaps (correlation, PII/path policy, test strategy); incorporated above.
