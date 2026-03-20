#!/usr/bin/env python3
"""
Extract control positions (Left, Top, Width, Height) from a TPF0 binary DFM.
Associates layout integer properties with the preceding control class and name
(TMainForm, TPanel, TSpeedButton, etc. + instance name). Used to document
exact UI layout for TSLPatcher parity; see docs/TSLPATCHER_RE.md §5.3.0.1.
"""

import sys
from pathlib import Path


def read_lp_string(data: bytes, offset: int) -> tuple[str | None, int]:
    """Read length-prefixed string; return (string or None, new_offset)."""
    if offset >= len(data):
        return None, offset
    length = data[offset]
    offset += 1
    if length == 0 or offset + length > len(data):
        return None, offset
    s = data[offset : offset + length].decode("latin-1", errors="replace")
    return s, offset + length


def extract_layout_controls(data: bytes) -> list[tuple[str, str, dict[str, int]]]:
    """Parse TPF0 and return list of (class_name, instance_name, {Left?, Top?, Width?, Height?})."""
    if len(data) < 4 or data[:4] != b"TPF0":
        return []
    i = 4
    result: list[tuple[str, str, dict[str, int]]] = []
    current: tuple[str, str, dict[str, int]] | None = None
    layout_props = ("Left", "Top", "Width", "Height")

    while i < len(data) - 1:
        s, i = read_lp_string(data, i)
        if s is None:
            i += 1
            continue
        # Control class: T + uppercase (TPanel, TButton, TSpeedButton, TMainForm, ...)
        if (
            len(s) >= 2
            and s[0] == "T"
            and s[1].isupper()
            and s.isascii()
            and s.isalpha()
        ):
            name, i = read_lp_string(data, i)
            if name and name.isascii() and name not in layout_props:
                if current and current[2]:
                    result.append(current)
                current = (s, name, {})
            continue
        # Layout property: 0x03 + 2-byte LE int
        if current is not None and s in layout_props and i + 3 <= len(data):
            if data[i] == 0x03:
                val = int.from_bytes(data[i + 1 : i + 3], "little")
                i += 3
                current[2][s] = val
                continue
        # Other properties: do not skip value (TPF0 has variable encodings); we only
        # reliably get the root form and any control whose next props are Left/Top/Width/Height.
        # Nested control positions (e.g. btnSummary, sbar) were obtained from a one-off scan
        # and are documented in docs/TSLPATCHER_RE.md §5.3.0.1.

    if current and current[2]:
        result.append(current)
    return result


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: python extract_tpf0_layout.py <dfm_file>", file=sys.stderr)
        return 1
    path = Path(sys.argv[1])
    if not path.is_file():
        print(f"Error: not a file: {path}", file=sys.stderr)
        return 1
    data = path.read_bytes()
    controls = extract_layout_controls(data)
    if not controls:
        print("No TPF0 layout data found.")
        return 0
    print("Class             Name        Left  Top   Width Height")
    print("-" * 55)
    for cls, name, props in controls:
        left = props.get("Left", "")
        top = props.get("Top", "")
        width = props.get("Width", "")
        height = props.get("Height", "")
        print(f"{cls:<17} {name:<11} {left!s:<5} {top!s:<5} {width!s:<5} {height!s:<5}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
