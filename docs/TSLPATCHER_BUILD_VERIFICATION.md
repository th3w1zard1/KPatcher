# TSLPatcher Vendor Build Verification

This document defines the canonical verification path for the reconstructed Delphi source tree under `vendor/TSLPatcher`.

## Recent RE Session — Execute_Pipeline Discovery (2026-03-21)

A Ghidra-based RE session on `/TSLPatcher.exe` (loaded into AgentDecompile MCP project) produced the following:

**Critical finding:** `Execute_Pipeline` @ **0x0047eec8** (1,981 bytes) — the top-level pipeline orchestrator — was found in the standard TSLPatcher.exe binary. It had been **zero-filled** in the HLFP modded binary used in prior sessions.

**Binary-confirmed pipeline execution order** (from `Execute_Pipeline` CALL instruction scan):
1. `CountModifications` (0x0047ebf8) — pre-pass; computes progress bar max
2. `PatchTLK` (0x00479850) — processes `[TLKList]`
3. `PatchGFF` (0x0048290c, 4,169 bytes) — processes `[GFFList]`
4. 2DA dispatch loop — processes `[2DAList]` by action key (`AddRow`/`ChangeRow`/`AddColumn`/`CopyRow`)
5. `ProcessInstallList` (0x0047c280) — processes `[InstallList]` (file copy)
6. `ProcessHACKList` (0x00482150) — processes `[HACKList]`
7. `CompileNSS` (0x0047d4ec) — processes `[CompileList]`; shells `nwnnsscomp.exe`
8. `PatchSSF` (0x0047e514) — processes `[SSFList]`

**Patch order correction:** The prior documented order (TLK → InstallList → 2DA → GFF → NSS → NCS → SSF) was an assumption. The confirmed binary order is **TLK → GFF → 2DA → InstallList → HACK → NSS → SSF**.

**26 pipeline functions** were created manually (Ghidra was not auto-analyzed; only 254 imported thunks were pre-recognized). Full function table: [docs/TSLPATCHER_RE.md § 5.2.5](TSLPATCHER_RE.md).

**Ghidra project note:** TSLPatcher.exe was NOT auto-analyzed when loaded. `DisassembleCommand` + `CreateFunctionCmd` were used to create all pipeline functions from known addresses. Run Ghidra UI **Analysis → Auto Analyze** to discover the full ~3,200+ function set.

## Scope

The vendor tree is a reverse-engineered Object Pascal reference for:

- `TSLPatcher.exe`
- `ChangeEdit.exe`

It exists to support parity work in the .NET codebase and to preserve the recovered structure, form layout, file-format handling, and UI/editor behavior of the original tools.

## Current Status

As of the current reconstruction pass:

- `25` `.pas` units are present under `vendor/TSLPatcher`
- `235+` methods are implemented
- no empty `begin { 0x... } end;` method stubs remain in the checked-in vendor tree
- the shared non-GUI layer compiles under Free Pascal 3.2.2 in Delphi mode
- `ProcessHACKList` fully implemented from RE of modded TSLPatcher binary (High Level Force Powers V2 patcher) including NCS bswap32 byte-swap

Validated shared units:

- `UTypes.pas`
- `UST_Common.pas`
- `UStrTok.pas`
- `UST_IniFile.pas`
- `UTLKFile.pas`
- `U2DAEdit.pas`
- `UGFFFile.pas`
- `UERFHandler.pas`
- `USSFFile.pas`
- `UTSLPatcher.pas`

## What Can Be Verified Locally

The repo verification script checks:

1. There are no empty reverse-engineering stubs left in `vendor/TSLPatcher`.
2. The shared non-GUI Pascal units compile with `fpc` using Delphi syntax mode.
3. The GUI projects are attempted only when a compatible GUI toolchain is available.

Run:

```powershell
pwsh ./scripts/Verify-TslpatcherVendor.ps1
```

## GUI Build Limitation

`TSLPatcher.dpr` and `ChangeEdit.dpr` depend on Delphi/Lazarus GUI units such as `Forms`, `Controls`, `Dialogs`, `ExtCtrls`, `ComCtrls`, `StdCtrls`, and `Grids`.

On this machine, `fpc` is installed but the GUI layer required for `Forms` is not available on the compiler search path, so full project builds stop at:

```text
Fatal: Can't find unit Forms used by ChangeEdit
```

That is an environment/toolchain limitation, not currently a proven syntax failure in the reconstructed GUI source itself.

## Canonical Maintenance Rules

The checked-in vendor tree is the canonical reviewed artifact.

Rules:

1. Do not overwrite `vendor/TSLPatcher` from bootstrap scripts without an explicit diff review.
2. Treat the reverse-engineered source in the repo as the source of truth unless and until the external generator is updated to emit the same content.
3. Any changes to the reconstruction should be followed by `pwsh ./scripts/Verify-TslpatcherVendor.ps1`.
4. If a local Delphi/Lazarus GUI toolchain becomes available, extend verification to full `.dpr` project builds.

## Regeneration Notes

Bootstrap extraction and generation tooling lives outside the repo working tree in the reverse-engineering workspace under `C:\temp\tslpatcher_re`.

Important:

- that tooling was used to create the initial baseline tree
- the checked-in tree has since been manually enriched beyond the original bootstrap output
- the bootstrap generator must not be treated as authoritative unless it has been updated to match the checked-in source

Relevant in-repo reference material:

- [vendor/TSLPatcher/README.md](../vendor/TSLPatcher/README.md)
- [docs/TSLPATCHER_UI_LAYOUT_EXACT.md](TSLPATCHER_UI_LAYOUT_EXACT.md)
- [scripts/README_extract_delphi_forms.md](scripts/README_extract_delphi_forms.md)

## Expected Workflow

1. Update or enrich the Pascal reference units.
2. Run `pwsh ./scripts/Verify-TslpatcherVendor.ps1`.
3. If shared-unit compilation fails, fix the Pascal source first.
4. If GUI compilation fails with missing `Forms`, record it as an environment limitation unless a GUI toolchain was expected.
5. Keep [vendor/TSLPatcher/README.md](vendor/TSLPatcher/README.md) aligned with this document.