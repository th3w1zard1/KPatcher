# KEditChanges

**ChangeEdit.exe** reverse-engineering mapping and parity. The binary is the reference for changes.ini editing and related editor behavior; equivalent logic is to live in this project.

- **ChangeEditReMapping.cs** — Ghidra/agdec addresses and metadata (entry 0x004b03a0, Borland Delphi, gzf path).

**Ghidra:** Same project as TSLPatcher/nwnnsscomp. Call `list-project-files`; if ChangeEdit.exe isn’t there, `open` with `path=C:/Users/boden/ChangeEdit.exe.gzf`. Then `open` with `path=/ChangeEdit.exe` to work on it.

**Build:** `dotnet build src/KEditChanges/KEditChanges.csproj`

**Mapping:** nwnnsscomp.exe -> **KCompiler** (KCompiler.Core). ChangeEdit.exe -> **KEditChanges** (this project).
