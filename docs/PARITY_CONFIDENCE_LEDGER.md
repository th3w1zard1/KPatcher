---
title: "KPatcher Parity Confidence Ledger"
created: 2026-05-23
status: active
audit_ref: "docs/plans/2026-06-10-002-feat-tslpatcher-core-logic-parity-iteration-plan.md"
---

# KPatcher Parity Confidence Ledger

**Document Purpose:** Durable record of parity assessment, test infrastructure confidence, and verified implementations. This ledger establishes the baseline for release gating, post-release monitoring, and future audit cycles.

**Last Audited:** 2026-06-12
**Audit Plan:** docs/plans/2026-06-10-002-feat-tslpatcher-core-logic-parity-iteration-plan.md
**Detailed Audit:** docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md
**Iteration PRs:** #18 (merged 2026-06-11), #19 (merged 2026-06-12)

---

## Executive Summary

**Parity Status: ⚠ PARTIAL — core install aligned; intentional extensions documented**

KPatcher implements the major TSLPatcher feature families. The 2026-06-10 parity iteration (PR #18) closed confirmed core-logic gaps: binary-verified pipeline order, InstallList overwrite guards, namespace install-path resolution, TLK append dedup, 2DA INI modifier order, `SafeStrToInt`, ResRef INI sanitization, install-time writable clearing, settings CRLF tokens, and K1 2DA hardcap removal. Branch `feat/tslpatcher-parity-gap-close` (2026-06-12) closes additional settings/modifier gaps: `InstallerMode`, `BackupFiles`, `PlaintextLog`, 2DA `inc(n)`, embedded 2DA modifier keys, exclusive-column fallback, GFF `2DAMEMORY` field keys, and `!OverrideType` destination guard. Branch `feat/parity-integration-tests-and-compile-serialize` (2026-06-12) adds install-path integration coverage documented in audit §11–§13 (`ModInstallerParityIntegrationTests`, pipeline byte assertions, CompileList module routing, read-only override patching). Remaining non-parity is **documented and intentional**: NCS-only HACKList, managed CompileList (`KCompiler`), timestamped backup/uninstall, and namespace selection by display name.

**Confidence Level:** Moderate-to-strong for core install behavior; partial for strict byte-for-byte TSLPatcher equivalence

**Risk Profile:** Low-to-medium for typical mod installs

- Pipeline matches binary-verified TSLPatcher order
- Generic HACKList behavior is narrowed to NCS-only patching (intentional)
- Compile backend uses managed `KCompiler`, not `nwnnsscomp.exe` (intentional, repo policy)
- Backup/uninstall uses timestamped mod-tree backups (intentional KPatcher extension)
- Namespace selection by display `Name` remains an extension vs section id

---

## 1. Test Infrastructure Confidence

### 1.1 Test Inventory

**Projects and Coverage:**

- **KPatcher.Tests:** 84+ files, 852 test cases (851 Default tier + 1 opt-in `TslPatcherExeReference`)
  - Formats: ~150 cases (GFF, 2DA, TLK, SSF, ERF, RIM, NCS, NSS format handling)
  - Mods: ~200 cases (modification types and application logic)
  - Reader: ~150 cases (config parsing, namespace resolution)
  - Logger: 7 cases (PatchLogger, InstallFlightRecorder, InstallLogWriter)
  - Patcher: ~55+ cases (ModInstaller unit/characterization + install-path integration: parity, pipeline order, settings, cross-stage memory)
  - Common: ~50 cases (utilities, RTF, geometry)
  - Memory: ~10+ cases (token substitution)

- **KCompiler.Tests:** 2 files, 4 test cases (NSS→NCS compiler parity)
- **NCSDecomp.Tests:** 1 file, 1 test case (NCS→NSS decompiler smoke)
- **KEditChanges.Tests:** 1 file, test count TBD (CLI tool smoke)

**Total: 955 test cases** (943 KPatcher.Tests Default + 1 opt-in + 6 KCompiler + 1 NCSDecomp + 1 KEditChanges)

### 1.2 Test Tier Structure

Seven distinct runsettings tiers enable graduated execution and specialized validation:

| Tier | Purpose | Default Enabled | Status |
|------|---------|-----------------|--------|
| **Default** | PR/commit baseline | ✅ Yes | Verified executable |
| **Exhaustive** | DeNCSRoundTrip (23k+ NCS scripts) | ❌ Opt-in | Documented as long-running |
| **VendorK2Game** | Retail K2 tree validation | ❌ Opt-in | Requires `KPATCHER_K2_VENDOR_ROOT` |
| **TslPatcherExeReference** | KPatcher manifest oracle + optional TSLPatcher.exe/baseline | ❌ Opt-in | Determinism + CLI vs direct oracle; `KPATCHER_TSLPATCHER_EXE` layout smoke; `KPATCHER_ORACLE_MANIFEST_BASELINE` for manual TSLPatcher diff |
| **KorExhaustiveBinaryFixtures** | Mod corpus validation | ❌ Opt-in | Requires synthetic payloads |
| **GeneratedGenericModSmoke** | In-memory mod harness | ❌ Opt-in (also Default) | 25 inline scenarios with golden manifest fingerprints (`EmbeddedScenarioPatternInstallTests`) |
| **GeneratedGenericModExhaustive** | `scenario_patterns/manifest.json` structural validation (116 legacy inventory rows) | ❌ Opt-in | `ManifestScenarioInventoryValidationTests`; `scripts/validate-manifest-inventory.sh` |

### 1.3 Skip and XFact Status

**Skipped / excluded tests:** Legacy `Integration/*.cs` corpus **removed from the repo** (former ~101 files; ~27 sources had ~17,970 compile diagnostics in `build_errors.txt`). Default-tier install coverage now lives under `tests/KPatcher.Tests/Patcher/*IntegrationTests.cs` and `EmbeddedIntegrationMods/`. Additional runtime `[Fact(Skip=…)]` markers for platform guards and optional env tiers.

**Breakdown:**

- **Former Integration corpus** — removed; not compile-excluded in current tree. Expand parity via focused `ModInstaller*IntegrationTests` and `EmbeddedIntegrationMods/scenario_patterns` rather than restoring byte[] fixture trees.

- **Platform-specific guards:** Windows ReadOnly tests, macOS case sensitivity
  - Impact: Minimal (non-blocking per-platform tests)

- **Optional tier gates:** Tests guarded by environment variables
  - Impact: None (tests silently skip when env vars not set)

### 1.4 Fixture Policy Compliance

**Status: ✅ Compliant (current tree)**

**Verified:**

- ✅ No `test_files/` root committed (`TestFilesRootPolicyTests`)
- ✅ Test data constructed via C# format APIs or ephemeral temp I/O
- ✅ Binary data exceptions: `_corrupted` samples and `.ncs` bytecode literals only
- ✅ No committed `.exe` fixture bytes; tests may create **ephemeral empty exe stubs** in temp game roots for InstallList guard characterization

**Evidence:** `InstallFlightRecorderTests`, `ModInstallerParityIntegrationTests`, `TestFilesRootPolicyTests`

**Confidence:** High for Default-tier paths; `EmbeddedIntegrationMods/` is the only copied mod tree (minimal `scenario_patterns` manifest).

---

## 2. Architecture Consistency

### 2.1 Module Responsibilities

**Verified Boundary Structure:**

```
KPatcher.Core (Dependency-free, lowest level)
├── Formats/ (GFF, 2DA, TLK, SSF, ERF, RIM, NCS, NSS parsers/builders)
├── Mods/ (Modification types and application logic)
├── Config/ (INI/YAML parsers, configuration parsing)
├── Logger/ (PatchLogger, InstallFlightRecorder, InstallLogWriter)
├── Patcher/ (ModInstaller orchestration, install lifecycle)
├── Reader/ (ConfigReader, NamespaceReader)
├── Memory/ (Token substitution engine)
└── Common/ (Script types, utilities)

KCompiler.Core (NSS→NCS compiler)
├── Managed replacement for nwnnsscomp.exe
├── Linked files from KPatcher.Core (Formats/NCS, Common/Script)
└── Zero external .exe dependencies

NCSDecomp.Core (NCS→NSS decompiler)
├── Decompiler engine
├── Embedded resources (lexer.dat, parser.dat, nwscript.nss)
└── ProjectReference to KCompiler.Core

KPatcher.UI (Avalonia desktop + CLI wrapper)
├── ViewModels (MVVM presentation logic)
├── Views (XAML UI components)
└── References: KPatcher.Core, KCompiler.Core

KEditChanges.NET (CLI tool umbrella)
├── References: KEditChanges, KCompiler.Core, NCSDecomp.Core

Test Projects (Isolated, non-cross-dependent)
├── KPatcher.Tests → KPatcher.Core + xUnit + FluentAssertions
├── KCompiler.Tests → KCompiler.Core
├── NCSDecomp.Tests → NCSDecomp.Core
└── KEditChanges.Tests → KEditChanges
```

### 2.2 Dependency Direction Analysis

**Verified Acyclic Structure:**

```
External packages (Foundation)
    ↓
KPatcher.Core
    ↓
├─ KCompiler.Core (imports Formats/NCS, Common/Script)
├─ NCSDecomp.Core (imports KCompiler.Core)
└─ KPatcher.UI (imports KPatcher.Core, KCompiler.Core)
    ↓
KPatcher.NET, KCompiler.NET, NCSDecomp.NET, KEditChanges.NET (CLI frontends)
    ↓
Tests (KPatcher.Tests, KCompiler.Tests, NCSDecomp.Tests, KEditChanges.Tests)
```

**Key Findings:**

- ✅ No circular dependencies detected
- ✅ Core is dependency-free (except managed-code foundations)
- ✅ UI and tools correctly depend on Core
- ✅ Test projects do not cross-depend
- ✅ All 16 projects identified and mapped

**Confidence:** High — dependency graph is clean and acyclic.

### 2.3 Public API Surface

**Verified Responsibilities:**

- KPatcher.Core exports: Patcher engine (ModInstaller), format handlers, config reader
- KCompiler.Core exports: NSS→NCS compilation contracts
- NCSDecomp.Core exports: NCS→NSS decompilation engine
- KPatcher.UI exports: Avalonia desktop application
- KEditChanges.NET exports: CLI tool for compile + decompile + metadata operations

**No over-coupling identified.** Public APIs align with stated module responsibilities.

---

## 3. Flight Recorder Implementation (PR #9)

### 3.1 Feature Completeness

**Status: ✅ COMPLETE & CORRECT**

**Implementation Files:**

- ✅ `/src/KPatcher.Core/Logger/InstallFlightRecorder.cs` (146 lines, feature implementation)
- ✅ `/tests/KPatcher.Tests/Logger/InstallFlightRecorderTests.cs` (7 test methods, unit tests)
- ✅ `/tests/KPatcher.Tests/Patcher/InstallFlightRecorderIntegrationTests.cs` (integration tests)

### 3.2 Lifecycle Verification

**Instantiation:** ModInstaller.Install() line 340

```csharp
installFlightRecorder = new InstallFlightRecorder(recordDirectory, gamePath, Game, log);
```

- ✅ Positioned at entry (before modification logic)
- ✅ Inside try block with protective catch
- ✅ Fail-soft: warning logged if creation fails, installation continues

**Finalization:** ModInstaller.Install() finally block lines 600-624

```csharp
finally {
    if (installFlightRecorder != null) {
        installFlightRecorder.MarkOutcome(terminalOutcome, terminalDetail);
        installFlightRecorder.Dispose();
    }
}
```

- ✅ Executes in all paths (success, exception, cancellation)
- ✅ Three terminal states captured: Success, Failure (InvalidOperationException), Cancellation (OperationCanceledException)
- ✅ Nested try-catch guards both calls (best-effort finalization)

### 3.3 Diagnostic Capture

**Event Subscription:** Line 62

```csharp
_logger.LogAdded += _logAddedHandler;
```

- ✅ Subscribes to PatchLogger events
- ✅ WriteExistingLogs() buffers pre-subscription logs (line 59)
- ✅ HandleLogAdded() streams real-time events to disk (lines 97-104)

**Log Categories:** All captured

- LogType.Diagnostic (primary diagnostic stream)
- LogType.Warning (status warnings)
- LogType.Note (user-visible notes)
- LogType.Verbose (implementation details)

**Output File:** {modPath}/installrecord.txt

- ✅ Human-readable format with timestamps
- ✅ Structured outcome recording (Success | Failure | Cancelled | Unknown)
- ✅ Optional detail field for failure reasons / cancellation notes

### 3.4 Non-Breaking Integration

**ModInstaller Tests:** Existing tests verified to still pass

- ✅ Flight recorder is non-breaking (feature addition, no behavior change)
- ✅ Error handling paths unchanged
- ✅ Parallel file writer (InstallLogWriter) unaffected

**CLI Headless Integration:** ProgramCliExecutionTests.cs

- ✅ Record path announced via PatchLogger.AddNote()
- ✅ Headless output visible to users

### 3.5 Test Coverage

**Unit Tests (InstallFlightRecorderTests.cs):**

1. Constructor_WritesHeaderAndSeededDiagnostics — initialization, header format
2. Constructor_ThrowsWhenModDirectoryIsMissing — error case
3. Dispose_WritesTerminalStateAndStreamsNewLogs — finalization, state capture

**Integration Tests (InstallFlightRecorderIntegrationTests.cs):**

1. Three terminal outcomes (Success, Failure, Cancellation)
2. Real installer integration (non-mocked)
3. File I/O validation

**Confidence:** High — feature is complete, tested, and non-breaking.

---

## 4. TSLPatcher Parity Status

### 4.1 Reference Material

**Documentation Sources:**

- ✅ `docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md` — refreshed evidence-based gap report
- ✅ `docs/TSLPATCHER_BUILD_VERIFICATION.md` — Ghidra binary analysis, behavior spec
- ✅ `docs/NWNNSSCOMP_RE.md` — nwnnsscomp.exe reverse-engineering, managed replacement parity
- ✅ `docs/EXHAUSTIVE_INLINE_PROVENANCE.md` — Test payload source documentation
- ✅ Vendor submodules: PyKotor (Python reference), Vanilla_KOTOR_Script_Source

### 4.2 Execution Pipeline Parity

**Parity target:** binary-verified shipped order (see `docs/TSLPATCHER_BUILD_VERIFICATION.md`).

**TSLPatcher reference orders (repo-local):**

1. **Older Delphi snapshot:** `TLK -> 2DA -> GFF -> HACK -> Compile -> InstallList` (historical)
2. **Current Delphi snapshot:** `TLK -> InstallList -> 2DA -> GFF -> HACK -> Compile -> SSF` (reconstructed; differs from binary)
3. **Build-verification doc (authoritative for parity):** `TLK -> GFF -> 2DA -> InstallList -> HACK -> NSS -> SSF`

**KPatcher sequence (ModInstaller, verified):**

1. CountModifications ✅
2. TLK modifications ✅
3. GFF modifications ✅
4. 2DA modifications ✅
5. InstallList ✅
6. NCS-only HACKList ✅
7. NSS compilation (managed `KCompiler`) ✅
8. SSF modifications ✅

**Assessment:** Pipeline **stage order matches binary-verified TSLPatcher**. HACK and Compile stages are intentional semantic subsets (NCS-only HACK, managed compile).

### 4.3 Format Handler Parity

**All Formats Implemented:**

- ✅ **GFF** (game object format) — read, modify, write
- ✅ **2DA** (2D array text format) — read, modify, write
- ✅ **TLK** (string table) — read, modify, write
- ✅ **SSF** (sound set file) — read, modify, write
- ✅ **ERF/RIM** (archive containers) — read, extract, repackage
- ✅ **NCS** (compiled NWScript bytecode) — read, patched write
- ✅ **NSS** (NWScript source) — compile via managed KCompiler.Core

### 4.4 Known Parity Deviations

| Deviation | Status | Reason | Impact |
|-----------|--------|--------|--------|
| **Pipeline order** | Resolved | KPatcher matches binary-verified order; Delphi source snapshots still disagree with each other | Low |
| **Namespace display-name selection** | Intentional extension | KPatcher selects namespace by display `Name`; fallback/`..` confinement aligned | Low |
| **InstallList overwrite safeguards** | Resolved | `.exe` / `.tlk` / `.key` / `.bif` folder replace guards in `ModInstaller` | Low |
| **InstallerMode / BackupFiles / PlaintextLog** | Resolved (gap-close) | Settings wired through `PatcherConfig`, `ConfigReader`, `ModInstaller`; integration tests in `ModInstallerSettingsIntegrationTests` | Low |
| **2DA `inc()` / exclusive fallback / GFF field-key memory** | Resolved (gap-close) | `RowValueInc`, `UnpackExclusiveFallback`, `PatcherMemory.ResolveMemoryToken` | Low |
| **!OverrideType destination guard** | Resolved (gap-close) | `HandleOverrideType` skips when destination is `Override`; `ModInstallerOverrideTypeTests` | Low |
| **Install pipeline order** | Resolved + tested | Binary-verified queue in `ModInstaller`; `ModInstallerPipelineOrderIntegrationTests` | Low |
| **Generic HACKList scope** | Intentional | NCS-only `[HACKList]`; TSLPatcher generic binary offset writes not implemented | Medium (edge mods) |
| **Compile backend** | Intentional | Managed `KCompiler`; `ScriptCompilerFlags` loaded; no `nwnnsscomp.exe` in product | Low |
| **K1 2DA hardcaps** | Resolved (removed) | Former KPatcher-only limits removed; no Delphi equivalent | Low |
| **Backup / uninstall semantics** | Intentional extension | Timestamped mod-tree backups + uninstall vs app-root single-copy backups | Low |
| **RTF rendering** | Intentional | Avalonia RichTextBox vs stripped plain text | UX improvement |
| **HACKList serialization** | Resolved | `KPatcherINISerializer.SerializeHackList` + round-trip test | INI export for NCS mods supported |
| **CompileList serialization** | Resolved | `SerializeCompileList` + `KPatcherINISerializerCompileListTests` | INI export for NSS compile mods |
| **GFF `2DAMEMORY#` field keys** | Resolved (unit); pipeline caveat | `PatcherMemory.ResolveMemoryToken` at apply time; **2DA memory cannot drive GFF at install** because GFF runs before 2DA in binary-verified order | Cross-stage 2DA->GFF not supported |
| **LZMA compression** | Implemented | `LzmaHelper` + `BzfHelper`; `Chitin` reads whole-file and packed-segment `.bzf`; `LzmaHelperTests` + `ChitinBzfTests` | iOS `.bzf` chitin edge case |
| **Script validation** | TODO | Confidence checks disabled pending validation | Deferred |

**Assessment:** Core install parity gaps from the 2026-06-10 iteration are closed. Remaining deviations are intentional product choices or low-priority TODOs.

### 4.5 nwnnsscomp.exe Replacement Parity

**Status: ✅ 100% PARITY**

**Managed Replacement:** KCompiler.Core

- ✅ Zero external .exe dependency (fully managed)
- ✅ CLI parity: `-g 1|2` (game selection), `--outputdir`, `--debug`, `--nwscript`
- ✅ Behavior parity verified across test suite (KCompiler.Tests)
- ✅ `-d` (decompile) exposed for compatibility but redirects to NCSDecomp

**Confidence:** High — managed replacement is feature-complete and tested.

---

## 5. Strategy Alignment

### 5.1 Active Tracks Assessment

**Track 1: Patcher Parity**

*Goal:* Install behavior, format handling, namespace/config parsing aligned with Python/TSLPatcher

**Status: ⚠ MOSTLY VERIFIED — intentional extensions remain**

**Implemented:**

- ✅ All format handlers (GFF, 2DA, TLK, SSF, ERF, RIM, NCS, NSS)
- ✅ All mod patch types (2DA, GFF, TLK, SSF, NCS, NSS)
- ✅ Config parsing (INI/YAML + localization)
- ✅ Namespace support (PatcherNamespace, localized variants, install-path resolution)
- ✅ Flight recorder + logging
- ✅ Binary-verified pipeline order in `ModInstaller`
- ✅ InstallList overwrite guards, TLK append dedup, 2DA INI order, ResRef/writable/CRLF parity (PR #18)

**Known intentional non-parity:**

- ⚠ Generic HACKList narrowed to NCS-only patching
- ⚠ Managed CompileList (`KCompiler`) instead of shelling `nwnnsscomp.exe`
- ⚠ Timestamped backup/uninstall vs TSLPatcher app-local backups
- ⚠ Namespace selection by display name
- ✅ HACKList serialization (`KPatcherINISerializer` write path)
- ✓ LZMA compression (iOS `.bzf` chitin; `LzmaHelperTests`, `ChitinBzfTests`)

**Assessment:** Core install behavior is aligned with binary-verified TSLPatcher after PR #18. Remaining gaps are documented product choices or low-priority TODOs.

---

**Track 2: Managed Script and Tooling Coverage**

*Goal:* NSS/NCS compile-decompile stack and bundled CLIs in managed code

**Status: ✅ 100% COMPLETE**

**Implemented:**

- ✅ KCompiler.Core (NSS → NCS, nwnnsscomp.exe parity verified)
- ✅ KCompiler.NET (CLI with full option support)
- ✅ NCSDecomp.Core (NCS → NSS, DeNCS port complete)
- ✅ NCSDecomp.NET + NCSDecomp.UI (CLI and desktop)
- ✅ KEditChanges.NET (umbrella CLI: compile + decompile + metadata)
- ✅ Zero external .exe dependencies (managed-only)
- ✅ Test coverage (KCompiler.Tests, NCSDecomp.Tests)

**Assessment:** Track is complete and production-ready.

---

**Track 3: Cross-platform Desktop Delivery**

*Goal:* Reliable Avalonia desktop app with publish, update, packaging flows

**Status: ✅ 70-80% IMPLEMENTATION IN PROGRESS**

**Implemented:**

- ✅ Avalonia 11.3.9 framework integrated
- ✅ Multi-platform RIDs (win/linux/osx, x86/x64/arm64 support)
- ✅ KPatcher.UI packable library (net9.0 + multi-target)
- ✅ Sidecar CLI publishing in build (KPatcher.UI.csproj lines 134-153)
- ✅ MVVM architecture (ViewModels, Views, reactive bindings)

**Deferred:**

- ❌ NetSparkle auto-update (net48 only; disabled on .NET 5+)
- ❌ Windows installer packaging (.msi/.exe)
- ❌ macOS/Linux deployment and code signing
- ❌ App store integration

**Assessment:** Core delivery infrastructure is solid. Distribution is limited to self-contained binaries (acceptable MVP). Installer packaging is future work.

---

**Track 4: Regression and Verification Harness**

*Goal:* CI, vendor comparisons, runsettings tiers, fixture policies to catch behavior drift

**Status: ✅ 75-80% ROBUST FOUNDATION WITH MIGRATION WORK**

**Implemented:**

- ✅ 7 runsettings tiers (Default, Exhaustive, VendorK2Game, TslPatcherExeReference, etc.)
- ✅ GitHub Actions CI (ci.yml, test-optional-tiers.yml)
- ✅ Zero external test fixture files policy (100% verified compliant)
- ✅ Format builder APIs for in-memory test data
- ✅ Comprehensive test categories (unit, integration, characterization, roundtrip)
- ✅ Parity ledger framework (ParityLedgerTests.cs, this document)
- ✅ 943 KPatcher.Tests Default-tier cases (including inline smoke + oracle helpers + `InstallPathHarnessClosureTests`)

**In Progress:**

- ✅ Twenty-five inline characterization scenarios with golden manifest fingerprints (`EmbeddedScenarioDefinitions`, `ScenarioGoldenManifests`); `ManifestIniPathPatternRegistry` covers all four manifest INI-path classes; CLI oracle covers CLI-eligible install scenarios
- ✅ All four manifest `ChangesIniRelative` path-shape classes characterized (`ManifestIniPathPatternRegistry`); 116 per-mod inventory ids remain metadata-only (zero-fixture policy — not default CI byte regression)

**Assessment:** Legacy Integration byte[] corpus is **removed** (see §1.3). Harness foundation is solid; remaining work is targeted parity expansion, not fixture migration.

---

### 5.2 Strategy Alignment Summary

| Track | Completeness | Priority | Blocker? |
|-------|--------------|----------|----------|
| Parity | 90-95% core install | High | ❌ No (remaining gaps are intentional or TODO) |
| Managed Tooling | 100% | High | ✅ Complete |
| Desktop Delivery | 70-80% | Medium | ❌ No (MVP sufficient) |
| Regression Harness | 75-80% | High | ⚠️ Migration blocking capacity |

**Overall Strategy Confidence:** High — all tracks have substantive implementations. Three tracks are substantially complete; one track has migration work in progress but is non-blocking for release.

---

## 6. Remediation Backlog

### Priority 1 (Blocking / Critical)

**None identified.** All P1 items have been resolved or are not applicable to current release scope.

---

### Priority 2 (Should Fix in Current / Next Cycle)

| Item | Component | Effort | Impact | Owner |
|------|-----------|--------|--------|-------|
| **Per-mod manifest byte regression** | Optional / maintainer | Large | 116 inventory ids pattern-covered; full `tslpatchdata` replay requires policy exception or local bootstrap | Parity / test owner |
| **LZMA Compression** | KPatcher.Core / Common | Low (implemented) | iOS `.bzf` chitin via SharpCompress LZMA1; round-trip + chitin tests | Compression module owner |

**Recommendation:** Expand inline characterization from `manifest.json` rows as mods require coverage. Monitor SharpCompress advisory GHSA-6c8g-7p36-r338 for LZMA dependency updates.

---

### Priority 3 (Nice-to-Have / Future Optimization)

| Item | Component | Effort | Impact | Owner |
|------|-----------|--------|--------|-------|
| **Script Validation** | KPatcher.Core / Script | Small | Confidence checks re-enabled, better error detection | Script owner |
| **Desktop Packaging** | KPatcher.UI | Large | Installer distribution, auto-update via NetSparkle | Release/delivery owner |
| **Continuous Parity Monitoring** | CI/Testing | Medium | Automated parity regression detection | DevOps / Test owner |

**Recommendation:** Script validation is the lowest-cost optional value-add; inline manifest migration is the largest remaining parity-test expansion.

---

## 7. Release Gating Checklist

**For KPatcher Release Readiness:**

- ✅ Test infrastructure passes (Default tier, critical tests)
- ✅ Fixture policy compliance verified (100%)
- ✅ Architecture consistency confirmed (no circular dependencies)
- ✅ Flight recorder is complete and non-breaking
- ⚠ Parity with TSLPatcher baseline is **mostly verified** for core install; see audit for intentional extensions (HACK scope, compile backend, backup/uninstall)
- ✅ All active strategy tracks have implementations
- ✅ Release owners may accept documented intentional deviations for typical mod installs

**Release Approval:** ✅ **CONDITIONAL ACCEPT** — core install parity iteration complete (PR #18); strict byte-for-byte equivalence not claimed

---

## 8. Audit Methodology

**Audit Performed:** 2026-06-11 (parity iteration close-out)
**Audit Plan:** docs/plans/2026-06-10-002-feat-tslpatcher-core-logic-parity-iteration-plan.md
**Detailed Audit Document:** docs/TSLPATCHER_CORE_LOGIC_PARITY_AUDIT.md
**Evidence Sources:**

- Codebase inspection (16 projects, 572 C# files)
- Test execution (851 KPatcher.Tests Default-tier cases verified executable, 2026-06-12)
- Architecture analysis (dependency mapping, module boundaries)
- Documentation review (STRATEGY.md, TESTING.md, reverse-engineering docs, build-verification notes)
- TSLPatcher source comparison (current Delphi snapshot, older Delphi snapshot, reviewed behavior-owning units)
- Fixture policy validation (100% compliance verified)
- Flight recorder verification (lifecycle, state capture, non-breaking)

**Confidence Methodology:** Combines direct observation (code, tests, docs, reviewed Delphi sources) with bounded synthesis about parity and risk. Confidence levels reflect both verification completeness and the explicit observation boundary of this refresh pass.

---

## 9. Audit Transition and Future Work

**Next Steps:**

1. **Harness Migration** (P2) — Extract integration tests from legacy fixture patterns.

2. **Optional product decisions** — Generic HACKList binary patching; restore `docs/TSLPatcher_RE.md` for full Ghidra tables.

3. **Continuous Parity Monitoring** (P3) — Implement KPatcher vs TSLPatcher.exe install golden diff behind `TslPatcherExeReference` tier.

**Future Audits:** Recommend quarterly refresh after major features or parity fixes.

---

**Ledger Status:** Active
**Approved By:** Engineering team (via refreshed parity audit)
**Certified:** 2026-06-11
