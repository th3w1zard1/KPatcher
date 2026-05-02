# nwnnsscomp.exe Reverse Engineering Status

This repository already contains a managed replacement for `nwnnsscomp.exe` and the reverse-engineering notes that justify it.

## Status

`nwnnsscomp.exe` has been reverse-engineered far enough for the repo's needs and is represented here as:

- binary-level mapping metadata in C#
- managed compiler behavior in `KCompiler.Core`
- a compatible CLI entry point in `KCompiler.NET`
- automated tests in `tests/KCompiler.Tests`

The repo policy is to prefer the managed implementation over invoking an external `nwnnsscomp.exe` binary.

## Source of Truth

Primary mapping file:

- [src/KCompiler.Core/NwnnsscompReMapping.cs](src/KCompiler.Core/NwnnsscompReMapping.cs)

Managed implementation:

- [src/KCompiler.Core/ManagedNwnnsscomp.cs](src/KCompiler.Core/ManagedNwnnsscomp.cs)
- [src/KCompiler.Core/NwnnsscompCliParser.cs](src/KCompiler.Core/NwnnsscompCliParser.cs)
- [src/KCompiler.NET/Program.cs](src/KCompiler.NET/Program.cs)

Binary-analysis reference:

- [docs/TSLPATCHER_RE.md](docs/TSLPATCHER_RE.md) section `§20`
- [docs/TSLPATCHER_RE.md](docs/TSLPATCHER_RE.md) appendix `C`

## Mapped Binary Facts

Known addresses from the original Windows binary:

- entry: `0x0041e6e4`
- main CLI: `0x004032da`
- compile single file: `0x00403075`
- open input file: `0x00402b64`

Known option codes:

- `0x63` = `-c`
- `0x64` = `-d`
- `0x65` = extra/debug-style option
- `0x6f` = `-o`

Known strings preserved in the mapping:

- usage banner
- product/banner text
- unrecognized option error
- too many arguments error
- unable to open input file error

## Managed Parity Outcome

The repo does not treat a Delphi/C port of `nwnnsscomp.exe` as the target artifact. Instead, the reverse engineering feeds a managed replacement that is easier to test and integrate.

That replacement currently provides:

- compile flow parity for the supported command-line surface
- `-g 1|2` game selection
- `--outputdir`
- `--debug`
- optional `--nwscript`

Current CLI note:

- `-d` is exposed for compatibility but is not implemented in `KCompiler.NET`; decompile functionality lives elsewhere in the repo via `NCSDecomp`.

## Verification

Validated locally:

```powershell
dotnet test tests/KCompiler.Tests/KCompiler.Tests.csproj
dotnet run --project src/KCompiler.NET/KCompiler.NET.csproj -- --help
```

At the time of this update:

- `KCompiler.Tests`: `4/4` passing
- `kcompiler --help`: successful startup and argument display

## Policy

The repo intentionally avoids depending on an external `nwnnsscomp.exe`.

Relevant policy and adjacent references:

- [.cursorrules](.cursorrules)
- [vendor/DeNCS](vendor/DeNCS)
- [vendor/PyKotor](vendor/PyKotor)

## Practical Conclusion

For this repository, `nwnnsscomp.exe` reverse engineering should be considered complete enough to support development and testing unless a new binary-only behavior gap is discovered.