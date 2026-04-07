# -*- coding: utf-8 -*-
"""Emit AnalysisAdapter.cs from vendor DeNCS AnalysisAdapter.java."""

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
    /// <summary>
    /// Base visitor that provides empty implementations for all grammar callbacks.
    /// Subclasses override only the nodes they care about while still being able to
    /// store per-node state via in/out maps.
    /// </summary>
"""

FOOTER = """
}
"""


def main():
    raw = JA.read_text(encoding="utf-8")
    # Strip Java header through opening brace of class
    i = raw.find("public class AnalysisAdapter")
    if i < 0:
        raise SystemExit("class not found")
    body = raw[i:]
    # Drop javadoc before class if any
    body = re.sub(r"/\*\*[\s\S]*?\*/\s*", "", body, count=1)
    body = body.replace("public class AnalysisAdapter implements Analysis {", "")
    # Remove closing brace of class (last line)
    body = body.rstrip()
    if body.endswith("}"):
        body = body[:-1].rstrip()

    body = body.replace("private Hashtable<Node, Object> in;", "private Hashtable _in;")
    body = body.replace("private Hashtable<Node, Object> out;", "private Hashtable _out;")
    body = re.sub(r"@Override\s*\n\s*", "", body)
    body = body.replace("this.in", "_in")
    body = body.replace("this.out", "_out")
    body = body.replace("Hashtable<Node, Object>", "Hashtable")
    body = body.replace("new Hashtable<>(1)", "new Hashtable()")

    body = body.replace(
        "public Object getIn(Node node) {",
        "public object GetIn(Node node)\n    {",
    )
    body = body.replace(
        "public void setIn(Node node, Object in) {",
        "public void SetIn(Node node, object value)\n    {",
    )
    body = body.replace("if (in != null) {", "if (value != null)\n        {")
    body = body.replace("this.in.put(node, in);", "_in[node] = value;")
    body = body.replace(
        "public Object getOut(Node node) {",
        "public object GetOut(Node node)\n    {",
    )
    body = body.replace(
        "public void setOut(Node node, Object out) {",
        "public void SetOut(Node node, object value)\n    {",
    )
    body = body.replace("if (out != null) {", "if (value != null)\n        {")
    body = body.replace("this.out.put(node, out);", "_out[node] = value;")

    body = body.replace("this.defaultCase(node);", "DefaultCase(node);")
    body = body.replace(
        "public void defaultCase(Node node) {", "public virtual void DefaultCase(Node node)\n    {"
    )

    body = re.sub(r"public void case(\w+)\(", r"public void Case\1(", body)

    # Normalize Java 3-space indent to 8-space (inside namespace + class)
    lines = []
    for line in body.splitlines():
        stripped = line.lstrip()
        if not stripped:
            lines.append("")
            continue
        js = len(line) - len(stripped)
        # Java body used 3-space base inside class -> map to 8 + (js-3)*1
        extra = max(0, js - 3)
        lines.append(" " * (8 + extra) + stripped)
    inner = "\n".join(lines)

    class_cs = "    public class AnalysisAdapter : Analysis\n    {\n" + inner + "\n    }"
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(HEADER + class_cs + FOOTER, encoding="utf-8")
    print("Wrote", OUT)


if __name__ == "__main__":
    main()
