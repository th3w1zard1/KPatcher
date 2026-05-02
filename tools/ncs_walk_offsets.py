"""Walk NCS V1.0 + 0x42 + BE size; 2-byte (opcode, qual) per insn; print offset of each insn start."""
import struct
import sys

path = sys.argv[1] if len(sys.argv) > 1 else r"c:\GitHub\KPatcher\test-work\roundtrip-work\k1\K1\Data\scripts.bif\k_act_atkonend.ncs"
data = open(path, "rb").read()
assert data[:8] == b"NCS V1.0"
assert data[8] == 0x42
size = struct.unpack(">I", data[9:13])[0]
pos = 13
idx = 0
targets = {2400, 2410, 2420, 2422, 2430}
while pos < size and pos < len(data):
    if pos in targets or (idx > 0 and pos - 2 in targets):
        pass
    op, q = data[pos], data[pos + 1]
    start = pos
    pos += 2
    extra = 0
    # Simplified: match NCSBinaryReader sizes (common)
    if op == 1:  # CPDOWNSP family - need qual to know - skip full parse
        extra = 6
    elif op == 4 and q == 3:  # CONSTI
        extra = 4
    elif op == 4 and q == 6:  # CONSTO
        extra = 4
    elif op == 4 and q == 2:  # CONSTF
        extra = 4
    elif op == 5:  # ACTION
        extra = 3
    elif op == 27:  # MOVSP
        extra = 4
    elif op in (29, 30, 31, 32):  # JMP JSR JZ JNZ - qual matters
        extra = 4
    elif op == 33:  # DESTRUCT
        extra = 6
    elif op == 28:  # STORE_STATE
        extra = 8
    else:
        extra = 0
    if start >= 2390 and start <= 2440:
        print(f"idx={idx} off={start} op={op:02x} q={q:02x} extra={extra}")
    pos += extra
    idx += 1
print("final pos", pos, "file len", len(data), "header size", size)
