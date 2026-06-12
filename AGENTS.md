# AGENTS.md

## Project overview

KPatcher is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. See `README.md` for full project details and `docs/` for additional documentation.

- `docs/solutions/` collects searchable writeups of past bugs, best practices, workflow patterns, and design decisions.

## Test fixture policy (ZERO external file dependencies)

- **All test data** must be defined and constructed ephemerally in `.cs` files — in memory at test time. There must be **zero committed test fixture files** on disk. The `test_files/` directory must not exist.
- **Binary data exceptions:** only `_corrupted`-suffixed samples and `.ncs` (compiled NWScript bytecode) may be defined as C# `byte[]` literals. `.exe` files are omitted entirely.
- **All other formats** (GFF/UTC/UTI/UTP/DLG/GIT/ARE/etc., 2DA, TLK, ERF/MOD/RIM, SSF, NSS source, INI, RTF) must be **constructed using format APIs** (`new GFF(GFFContent.UTC)`, `new TwoDA(columns)`, `new TLK(Language.English)`, `new ERF(ERFType.MOD)`, etc.) or defined as plaintext string constants.
- Details: [docs/TESTING.md](docs/TESTING.md).

## Cursor Cloud specific instructions

### Gotchas 

- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.

## Learned User Preferences

- Investigate and define scope before implementing; do not jump to code without understanding the end goal (especially for `/lfg` and parity work).
- When porting TSLPatcher logic, prefer newer or better KPatcher code overall while preserving core behavior and avoiding regressions.
- For TSLPatcher↔KPatcher parity, alternate reading Pascal units under `vendor/TSLPatcher` and equivalent C# under `src/KPatcher.Core` instead of bulk-reading one side first.
- When integration or parity work is active, merge open PRs and feature branches into the default branch rather than leaving parallel drift.

## Learned Workspace Facts

- Authoritative repository checkout is `/run/media/brunner56/MyBook/Workspaces/KPatcher` (workspace and git root resolve here).
- TSLPatcher parity baseline is the Pascal/Delphi source in `vendor/TSLPatcher` (OpenKotor); compare it to KPatcher C# iteratively, not binary RE alone.
- Product NSS/NCS tooling is fully managed: `KCompiler.Core` for NSS→NCS, `NCSDecomp.Core` for NCS→NSS; no registry spoofer and no `nwnnsscomp.exe` in product or default CI paths.
- `ModInstaller` defaults `TslPatchDataPath` to the `changes.ini` directory when callers omit it (HACKList and tslpatchdata asset resolution).
- Install-path characterization harness is **complete** (25 inline scenarios, golden manifest oracle, CLI vs direct oracle, manifest INI-path pattern map, exhaustive-tier inventory validation). Gate: `InstallPathHarnessClosureTests` + `bash ./scripts/validate-manifest-inventory.sh`. See [docs/TESTING.md](docs/TESTING.md) and [docs/TSLPATCHER_RE.md](docs/TSLPATCHER_RE.md). Remaining parity gaps are **product scope** (LZMA `.bzf`, generic binary HACKList, per-mod byte replay of 116 manifest ids) — not missing harness infrastructure.
