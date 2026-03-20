#!/usr/bin/env python3
"""
Parse TPF0 (Lazarus/CodeTyphon) binary DFM format to extract key properties.
TPF0 format: 4-byte signature "TPF0", then length-prefixed property names and values.
"""

import sys
from pathlib import Path
from typing import Any


def read_string(data: bytes, offset: int) -> tuple[str, int]:
    """Read length-prefixed string, return (string, new_offset)."""
    if offset >= len(data):
        return "", offset
    length = data[offset]
    offset += 1
    if offset + length > len(data):
        return "", offset
    s = data[offset:offset + length].decode('latin-1', errors='replace')
    return s, offset + length


def read_uint16_le(data: bytes, offset: int) -> tuple[int, int]:
    """Read little-endian uint16, return (value, new_offset)."""
    if offset + 2 > len(data):
        return 0, offset
    val = int.from_bytes(data[offset:offset + 2], 'little')
    return val, offset + 2


def parse_tpf0(data: bytes) -> dict[str, Any]:
    """Parse TPF0 binary DFM, return dict of key properties."""
    if len(data) < 4 or data[:4] != b'TPF0':
        return {}
    
    result = {}
    offset = 4
    
    # Read form class name
    form_class, offset = read_string(data, offset)
    if form_class:
        result['FormClass'] = form_class
    
    # Read form name
    form_name, offset = read_string(data, offset)
    if form_name:
        result['FormName'] = form_name
    
    # Parse properties
    while offset < len(data):
        prop_name, offset = read_string(data, offset)
        if not prop_name:
            break
        
        # Skip value type/length indicators and read value
        if offset >= len(data):
            break
        
        # Common property types:
        # - Integer (2 bytes): 0x03 prefix, then 2-byte LE value
        # - String: 0x06 prefix, then length-prefixed string
        # - Boolean: 0x01 or 0x00
        # - Set/enum: various
        
        value_type = data[offset] if offset < len(data) else 0
        offset += 1
        
        if value_type == 0x03 and offset + 2 <= len(data):  # Integer (2-byte)
            val, offset = read_uint16_le(data, offset)
            result[prop_name] = val
        elif value_type == 0x06:  # String
            val, offset = read_string(data, offset)
            result[prop_name] = val
        elif value_type in (0x00, 0x01):  # Boolean
            result[prop_name] = bool(value_type)
            # offset already advanced
        else:
            # Unknown type, try to skip
            if offset < len(data):
                # Heuristic: if next byte looks like a property name length, we're done with this value
                next_byte = data[offset] if offset < len(data) else 0
                if 0x20 <= next_byte <= 0x7E:  # Printable ASCII, might be next property
                    # Actually, let's try reading as string if reasonable length
                    if next_byte < 50:
                        try:
                            val, new_offset = read_string(data, offset)
                            if val and all(32 <= ord(c) <= 126 for c in val[:20]):
                                result[prop_name] = val
                                offset = new_offset
                                continue
                        except:
                            pass
                # Skip unknown value - advance by 1-4 bytes heuristically
                offset += 1
    
    return result


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: python parse_tpf0_dfm.py <dfm_file>", file=sys.stderr)
        return 1
    
    dfm_path = Path(sys.argv[1])
    if not dfm_path.is_file():
        print(f"Error: not a file: {dfm_path}", file=sys.stderr)
        return 1
    
    data = dfm_path.read_bytes()
    props = parse_tpf0(data)
    
    if not props:
        print("No properties extracted (not TPF0 format?)")
        return 1
    
    # Print key layout properties
    layout_props = ['Left', 'Top', 'Width', 'Height', 'Caption', 'FormClass', 'FormName']
    for prop in layout_props:
        if prop in props:
            print(f"{prop}: {props[prop]}")
    
    # Print all properties (skip non-printable values)
    print("\nAll properties:")
    for key, value in sorted(props.items()):
        try:
            print(f"  {key}: {value}")
        except UnicodeEncodeError:
            print(f"  {key}: <binary data>")
    
    return 0


if __name__ == "__main__":
    sys.exit(main())
