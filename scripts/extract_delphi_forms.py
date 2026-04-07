#!/usr/bin/env python3
"""
Extract Delphi form (DFM) resources from a PE file.
Delphi stores forms as RCDATA resources; each resource name is the form class (e.g. TMAINFORM).
Output: raw binary .dfm files. Binary DFM can start with 'TPF0' (Lazarus) or 0xFF (older Delphi).
Usage: python extract_delphi_forms.py <exe_path> [output_dir]
"""

from __future__ import annotations

import argparse
import re
import sys

from pathlib import Path


def safe_filename(name: str) -> str:
    """Make a safe filename from resource name."""
    if not name:
        return "unnamed"
    return re.sub(r"[^\w\-.]", "_", name)


def extract_rcdata(pe) -> list[tuple[str, bytes]]:
    """Yield (resource_name, data) for each RCDATA resource."""
    # RT_RCDATA = 10
    RCDATA_TYPE = 10
    if not hasattr(pe, "DIRECTORY_ENTRY_RESOURCE"):
        return []
    results = []
    for type_entry in pe.DIRECTORY_ENTRY_RESOURCE.entries:
        if type_entry.struct.Id != RCDATA_TYPE:
            continue
        if not hasattr(type_entry, "directory") or not type_entry.directory:
            continue
        for name_entry in type_entry.directory.entries:
            name = str(name_entry.name) if name_entry.name else f"id_{name_entry.struct.Id}"
            if not hasattr(name_entry, "directory") or not name_entry.directory:
                continue
            for lang_entry in name_entry.directory.entries:
                if not hasattr(lang_entry, "data") or lang_entry.data is None:
                    continue
                struct = lang_entry.data.struct
                rva = struct.OffsetToData
                size = struct.Size
                try:
                    data = pe.get_data(rva, size)
                except Exception:
                    continue
                results.append((name, data))
    return results


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract Delphi form (RCDATA) resources from a PE file."
    )
    parser.add_argument("exe", type=Path, help="Path to the executable (e.g. TSLPatcher.exe)")
    parser.add_argument(
        "output_dir",
        type=Path,
        nargs="?",
        default=None,
        help="Output directory (default: <exe_stem>_forms next to exe)",
    )
    args = parser.parse_args()

    exe = args.exe.resolve()
    if not exe.is_file():
        print(f"Error: not a file: {exe}", file=sys.stderr)
        return 1

    try:
        import pefile
    except ImportError:
        print("Error: pefile is required. Install with: pip install pefile", file=sys.stderr)
        return 1

    out_dir = args.output_dir
    if out_dir is None:
        out_dir = exe.parent / f"{exe.stem}_forms"
    out_dir = out_dir.resolve()
    out_dir.mkdir(parents=True, exist_ok=True)

    pe = pefile.PE(str(exe), fast_load=True)
    pe.parse_data_directories(
        directories=[pefile.DIRECTORY_ENTRY["IMAGE_DIRECTORY_ENTRY_RESOURCE"]]
    )

    items = extract_rcdata(pe)
    pe.close()

    if not items:
        print("No RCDATA resources found.")
        return 0

    for name, data in items:
        safe = safe_filename(name)
        ext = "dfm"
        path = out_dir / f"{safe}.{ext}"
        path.write_bytes(data)
        sig = data[:4] if len(data) >= 4 else data
        sig_desc = (
            "binary (TPF0)"
            if sig == b"TPF0"
            else ("binary (0xFF)" if (data[0:1] == b"\xff") else "binary/unknown")
        )
        print(f"Saved: {path} ({len(data)} bytes, {sig_desc})")

    print(f"\nExtracted {len(items)} form(s) to {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
