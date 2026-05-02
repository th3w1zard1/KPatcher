# AGENTS.md

## Project overview

KPatcher is a C#/.NET Avalonia desktop application for installing Star Wars KOTOR mods. See `README.md` for full project details and `docs/` for additional documentation.

## Test fixture policy (ZERO external file dependencies)

- **All test data** must be defined and constructed ephemerally in `.cs` files — in memory at test time. There must be **zero committed test fixture files** on disk. The `test_files/` directory must not exist.
- **Binary data exceptions:** only `_corrupted`-suffixed samples and `.ncs` (compiled NWScript bytecode) may be defined as C# `byte[]` literals. `.exe` files are omitted entirely.
- **All other formats** (GFF/UTC/UTI/UTP/DLG/GIT/ARE/etc., 2DA, TLK, ERF/MOD/RIM, SSF, NSS source, INI, RTF) must be **constructed using format APIs** (`new GFF(GFFContent.UTC)`, `new TwoDA(columns)`, `new TLK(Language.English)`, `new ERF(ERFType.MOD)`, etc.) or defined as plaintext string constants.
- Details: [docs/TESTING.md](docs/TESTING.md).

## Cursor Cloud specific instructions

### Gotchas 

- **.NET SDK is installed at `$HOME/.dotnet`**, which is added to `PATH` via `~/.bashrc`. The update script also ensures this.
- **Avalonia GUI** requires `DISPLAY=:1` environment variable to launch the X11 window.
