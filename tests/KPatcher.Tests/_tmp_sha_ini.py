import hashlib
import re
from pathlib import Path

def digest(path: str, start: str, end: str) -> str:
    t = Path(path).read_text(encoding="utf-8")
    m = re.search(start + r"(.*?)" + end, t, re.DOTALL)
    if not m:
        raise SystemExit(f"no match in {path}")
    s = m.group(1).replace('""', '"')
    return hashlib.sha256(s.encode("utf-8")).hexdigest()


base = Path(__file__).resolve().parent / "Integration/EmbeddedTslpatcherArchiveCorpus"
# C# verbatim strings end with "; (quote + semicolon), then newline.
_end_next = r'";\s*\r?\n\s*internal const string '

print(
    "delta",
    digest(
        base / "K1ArchiveCorpusCaseDeltaFixture.cs",
        r'internal const string TslpatchdataChangesIni = @"',
        _end_next + "ReadmeTxt",
    ),
)
print(
    "epsilon",
    digest(
        base / "TslArchiveCorpusCaseEpsilonFixture.cs",
        r'internal const string TslpatchdataChangesIni = @"',
        _end_next + "ReadmeTxt",
    ),
)
print(
    "beta",
    digest(
        base / "TslArchiveCorpusCaseBetaFixture.cs",
        r'internal const string ChangesIni = @"',
        _end_next + "SourceNss",
    ),
)
