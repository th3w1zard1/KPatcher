# -*- coding: utf-8 -*-
import re

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
JA = (
    ROOT / "vendor/DeNCS/src/main/java/com/kotor/resource/formats/ncs/analysis/AnalysisAdapter.java"
)
OUT = ROOT / "src/NCSDecomp.Core/Analysis/AnalysisAdapter.cs"

HEADER = """// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System.Collections;
using NCSDecomp.Core.Node;

namespace NCSDecomp.Core.Analysis
{
"""

FOOTER = """
}
"""


def main():
    s = JA.read_text(encoding="utf-8")
    s = re.sub(r"// Copyright[\s\S]*?information\.\s*\n", "", s, count=1)
    s = re.sub(r"package com\.kotor\.resource\.formats\.ncs\.analysis;\s*", "", s)
    s = re.sub(r"import com\.kotor\.resource\.formats\.ncs\.node\.\w+;\s*", "", s)
    s = re.sub(r"import java\.util\.Hashtable;\s*", "", s)
    s = re.sub(r"@Override\s*\n\s*", "", s)
    s = s.replace(
        "public class AnalysisAdapter implements Analysis",
        "public class AnalysisAdapter : Analysis",
    )
    s = s.replace("private Hashtable<Node, Object> in;", "private Hashtable _in;")
    s = s.replace("private Hashtable<Node, Object> out;", "private Hashtable _out;")
    s = s.replace("this.in", "_in")
    s = s.replace("this.out", "_out")
    s = s.replace("Hashtable<Node, Object>", "Hashtable")
    s = s.replace("new Hashtable<>(1)", "new Hashtable()")
    s = s.replace("Object getIn(Node node)", "public object GetIn(Node node)")
    s = s.replace("void setIn(Node node, Object in)", "public void SetIn(Node node, object @in)")
    s = s.replace("Object getOut(Node node)", "public object GetOut(Node node)")
    s = s.replace(
        "void setOut(Node node, Object out)", "public void SetOut(Node node, object @out)"
    )
    s = re.sub(r"public void case(\w+)\(", r"public void Case\1(", s)
    s = s.replace("this.defaultCase(node);", "DefaultCase(node);")
    s = s.replace(
        "public void defaultCase(Node node)", "public virtual void DefaultCase(Node node)"
    )
    s = s.replace("   }", "    }")
    s = s.replace("   public", "    public")
    s = s.replace("   if (", "        if (")
    s = s.replace("   } else", "        } else")
    s = s.replace("   }", "        }")
    # Fix botched indentation from blind replace - rewrite body with simpler approach
    body_start = s.find("public class AnalysisAdapter")
    class_block = s[body_start:]
    # Re-indent: Java uses 3 spaces, normalize to 4 inside namespace
    lines = class_block.splitlines()
    out_lines = []
    for line in lines:
        if line.strip() == "":
            out_lines.append("")
            continue
        stripped = line.lstrip()
        indent = len(line) - len(stripped)
        new_indent = 4 + max(0, indent - 3)
        out_lines.append(" " * new_indent + stripped)
    class_cs = "\n".join(out_lines)
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(HEADER + class_cs + "\n" + FOOTER, encoding="utf-8")
    print("Wrote", OUT)


if __name__ == "__main__":
    main()
