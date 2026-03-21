# -*- coding: utf-8 -*-
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
JA = ROOT / "vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/analysis/Analysis.java"
OUT = ROOT / "src/NCSDecomp.Core/Analysis/Analysis.cs"

HEADER = """// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Analysis
{
    /// <summary>
    /// Visitor contract (SableCC analysis) for walking the decompiler AST.
    /// </summary>
    public interface IAnalysis : Switch
    {
"""

FOOTER = """
    }
}
"""


def main():
    raw = JA.read_text(encoding="utf-8")
    # Extract lines inside interface Analysis
    m = re.search(r"public interface Analysis extends Switch \{([\s\S]*)\}", raw)
    if not m:
        raise SystemExit("interface not found")
    body = m.group(1)
    lines = []
    for line in body.splitlines():
        line = line.strip()
        if not line:
            continue
        # Object getIn -> object GetIn
        line = line.replace("Object getIn", "object GetIn")
        line = line.replace("void setIn", "void SetIn")
        line = line.replace("Object getOut", "object GetOut")
        line = line.replace("void setOut", "void SetOut")
        line = re.sub(r"void case(\w+)\(", r"void Case\1(", line)
        line = line.replace("var1", "node")
        line = line.replace("var2", "value")
        lines.append("        " + line)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(HEADER + "\n".join(lines) + FOOTER, encoding="utf-8")
    print("Wrote", OUT)


if __name__ == "__main__":
    main()
