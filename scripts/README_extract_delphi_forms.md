# Extracting Delphi form resources (DFM)

## Purpose

Extract Delphi form (DFM) resources from a PE file (e.g. **TSLPatcher.exe**) to obtain exact window layout, size, and control properties (Width, Height, Left, Top, Caption, etc.) for UI parity.

## Requirements

- Python 3.7+
- **pefile**: `pip install pefile`
- The target executable (e.g. TSLPatcher.exe). It is not in this repo; obtain it from a KOTOR/TSL mod that ships TSLPatcher or from your Ghidra/analysis copy.

## Usage

```bash
# From repo root
pip install pefile
python scripts/extract_delphi_forms.py <path_to_exe> [output_dir]
```

**Examples:**

```bash
python scripts/extract_delphi_forms.py "C:\Path\To\TSLPatcher.exe"
# -> creates C:\Path\To\TSLPatcher_forms\ with TMAINFORM.dfm, TNAMESPACEFORM.dfm, etc.

python scripts/extract_delphi_forms.py "C:\Path\To\TSLPatcher.exe" docs/tslpatcher_forms
# -> writes .dfm files into docs/tslpatcher_forms
```

## Output

- One `.dfm` file per RCDATA resource; filename = resource name (e.g. `TMAINFORM.dfm`, `TNAMESPACEFORM.dfm`, `PACKAGEINFO.dfm`, `DVCLAL.dfm`).
- **TMAINFORM / TNAMESPACEFORM:** Binary DFM (often TPF0). To get readable text (Width, Height, Left, Top, etc.):
  - Use **Delphi** `convert.exe` (ObjectBinaryToText) if you have Delphi installed.
  - Or open the .dfm in **Lazarus/CodeTyphon** (or build [DfmExtractor](https://github.com/jackdp/DfmExtractor) with Lazarus + JCL).
  - Or use **Resource Hacker** / **NirSoft ResourcesExtract** to export resources, then convert binary DFM with an external tool.

## Parsing binary DFM (TPF0)

For TPF0 binary DFMs (e.g. TSLPatcher), use the bundled parser to list properties:

```bash
python scripts/parse_tpf0_dfm.py docs/tslpatcher_forms/TMAINFORM.dfm
```

This prints Left, Top, Width, Height, Caption, FormClass, FormName, and other properties.

**Nested layout (control positions):**

```bash
python scripts/extract_tpf0_layout.py docs/tslpatcher_forms/TMAINFORM.dfm
```

Prints a table of control class, name, and Left/Top/Width/Height. Reliably reproduces the **root form** (TMainForm MainForm 384×228×566×499). Nested control positions (e.g. btnSummary, sbar) are documented in **docs/TSLPATCHER_RE.md** §5.3.0.1 and **docs/TSLPATCHER_UI_LAYOUT_EXACT.md** (from a one-off scan).

**PACKAGEINFO.dfm** is not TPF0; scan for ASCII strings to get package/unit names (UMainForm, UTSLPatcher, UGFFFile, etc.). **DVCLAL.dfm** is a 16-byte binary blob (VCL alignment).

## Where this is documented

- **docs/TSLPATCHER_RE.md** — §5.3.0 (extracting form resources, exact window layout), §5.3.0.1 (full reference: RCDATA inventory, TMAINFORM/TNAMESPACEFORM, PACKAGEINFO unit list, DVCLAL), Appendix B (extracted form resources table); agdec-mcp locations for form names/registration.
- **docs/TSLPATCHER_UI_LAYOUT_EXACT.md** — Summary of extracted layouts and KPatcher parity.
