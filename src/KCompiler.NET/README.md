# KCompiler.NET

Cross-platform CLI tool for compiling NSS scripts to NCS bytecode. A managed, drop-in alternative to `nwnnsscomp.exe` that works on Windows, Linux, and macOS.

## Installation

Install as a .NET tool:

```bash
dotnet tool install -g KCompiler.NET
```

## Usage

```bash
kcompiler -c script.nss -o output.ncs -g 1
```

### Arguments

- `-c`, `--compile <file>`: NSS source file to compile
- `-o`, `--output <file>`: Output NCS file path
- `-g <1|2>`: Game version (1 = KOTOR, 2 = TSL)
- `--outputdir <dir>`: Output directory (alternative to `-o`)
- `--debug`: Include debug symbols
- `--nwscript <file>`: Custom nwscript.nss path
- `-h`, `--help`: Show help

## Examples

```bash
# Compile for KOTOR
kcompiler -c my_script.nss -o my_script.ncs -g 1

# Compile for TSL with debug symbols
kcompiler -c script.nss -o script.ncs -g 2 --debug

# Output to directory
kcompiler -c script.nss --outputdir ./compiled -g 1
```

## See Also

- **KCompiler.Core**: The underlying library (NuGet package) that provides the compilation API.
