# KEditChanges

Managed port of [OpenKotOR/ChangeEdit](https://github.com/OpenKotOR/ChangeEdit) — the TSLPatcher **changes.ini** editor.

| ChangeEdit (Delphi) | KEditChanges (.NET) |
|---------------------|---------------------|
| `UST_IniFile` escapes (`<#LF#>`, `<#CR#>`) | `Ini/TstIniEscapes.cs` |
| `TST_IniFile` / live document | `ChangesIniDocument` + `ChangesIniService` |
| `UMainForm` section tree | `KEditChanges.UI` (Avalonia) |
| Binary GFF/TLK/2DA I/O | **KPatcher.Core** format APIs (not re-ported) |
| Patch INI serialization | `Ini/ChangeEditIniWriter` + `KPatcherINISerializer` |

## Projects

- **`KEditChanges`** — core library (load/validate/save/summary, CLI helpers).
- **`KEditChanges.UI`** — Avalonia desktop editor (`KEditChangesUI`).
- **`KEditChanges.NET`** — umbrella CLI `keditchanges-cli` (compile, decomp, **validate/summary/serialize**).

## Build

```bash
dotnet build src/KEditChanges/KEditChanges.csproj
dotnet build src/KEditChanges.UI/KEditChanges.UI.csproj
dotnet build src/KEditChanges.NET/KEditChanges.NET.csproj
```

## CLI (agent-native)

```bash
keditchanges-cli validate -i path/to/changes.ini
keditchanges-cli summary -i path/to/changes.ini
keditchanges-cli serialize -i path/to/changes.ini -o path/to/out.ini
keditchanges-cli info
```

## Reverse engineering

- **`ChangeEditReMapping.cs`** — Ghidra/agdec metadata (entry `0x004b03a0`, Borland Delphi 7).
- Ghidra project hint: `ChangeEdit.exe.gzf` (same project family as TSLPatcher/nwnnsscomp).

## Status

Core INI document I/O and Settings UI are implemented. Full parity with every ChangeEdit modal dialog (2DA row editor, GFF field tree, mass add, namespaces, etc.) is tracked incrementally — use CLI round-trip for automation until dedicated section panels ship.
